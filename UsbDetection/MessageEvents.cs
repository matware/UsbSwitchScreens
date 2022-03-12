using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;
namespace UsbNotify
{
    public static class MessageEvents
    {
        private static SynchronizationContext context;
        public static event Action<Message> MessageReceived;
        public static event Action FormClosing;

        private static MessageWindow window;
        private static IntPtr windowHandle;        

        public static void WatchMessage(int message)
        {
            EnsureInitialized();
            window.RegisterEventForMessage(message);
        }

        public static IntPtr WindowHandle
        {
            get
            {
                EnsureInitialized();
                return windowHandle;
            }
        }
        private static object _lock = new object();

        private static void EnsureInitialized()
        {
            lock (_lock)
            {
                if (window == null)
                {
                    context = AsyncOperationManager.SynchronizationContext;
                    using (ManualResetEvent mre = new ManualResetEvent(false))
                    {
                        Thread t = new Thread((ThreadStart)delegate
                        {
                            window = new MessageWindow();
                            window.FormClosing += window_FormClosing;
                            windowHandle = window.Handle;
                            mre.Set();
                            Application.Run();
                        });
                        t.Name = "MessageEvents";
                        t.IsBackground = true;
                        t.Start();

                        mre.WaitOne();
                    }
                }
            }
        }

        private static void window_FormClosing(object sender, FormClosingEventArgs e)
        {
            FormClosing?.Invoke();
        }

        private class MessageWindow : Form
        {
            private ReaderWriterLock rwLock = new ReaderWriterLock();
            private HashSet<int> messageSet =new HashSet<int>();

            public void RegisterEventForMessage(int messageID)
            {
                rwLock.AcquireWriterLock(Timeout.Infinite);
                messageSet.Add(messageID);
                rwLock.ReleaseWriterLock();
            }

            protected override void WndProc(ref Message m)
            {
                rwLock.AcquireReaderLock(Timeout.Infinite);
                bool handleMessage = messageSet.Contains(m.Msg);
                rwLock.ReleaseReaderLock();
                if(m.Msg == 0x16)
                    System.IO.File.WriteAllText("boom0.txt", $"boom0 {m.Msg} {m.WParam} {m.LParam}");
                if (handleMessage)
                {
                    MessageEvents.context.Post(delegate (object state)
                    {
                        var handler = MessageEvents.MessageReceived;
                        if (handler != null)
                        {
                            var message = (Message)state;
                            handler(message);
                        }
                    }, m);
                }
                base.WndProc(ref m);
            }
        }
    }
}