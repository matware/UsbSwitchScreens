using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;
namespace UsbNotify
{
    public static class MessageEvents
    {
        private static SynchronizationContext _context;
        public static event Action<Message> MessageReceived;
        public static event Action FormClosing;

        private static MessageWindow _window;
        private static IntPtr _windowHandle;        

        public static void WatchMessage(int message)
        {
            EnsureInitialized();
            _window.RegisterEventForMessage(message);
        }

        public static IntPtr WindowHandle
        {
            get
            {
                EnsureInitialized();
                return _windowHandle;
            }
        }
        private static object _lock = new object();

        private static void EnsureInitialized()
        {
            lock (_lock)
            {
                if (_window == null)
                {
                    _context = AsyncOperationManager.SynchronizationContext;
                    using (ManualResetEvent mre = new ManualResetEvent(false))
                    {
                        Thread t = new Thread((ThreadStart)delegate
                        {
                            _window = new MessageWindow();
                            _window.FormClosing += _window_FormClosing;
                            _windowHandle = _window.Handle;
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

        private static void _window_FormClosing(object sender, FormClosingEventArgs e)
        {
            FormClosing?.Invoke();
        }

        private class MessageWindow : Form
        {
            private ReaderWriterLock _lock = new ReaderWriterLock();
            private HashSet<int> _messageSet =new HashSet<int>();

            public void RegisterEventForMessage(int messageID)
            {
                _lock.AcquireWriterLock(Timeout.Infinite);
                _messageSet.Add(messageID);
                _lock.ReleaseWriterLock();
            }

            protected override void WndProc(ref Message m)
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                bool handleMessage = _messageSet.Contains(m.Msg);
                _lock.ReleaseReaderLock();
                if(m.Msg == 0x16)
                    System.IO.File.WriteAllText("boom0.txt", $"boom0 {m.Msg} {m.WParam} {m.LParam}");
                if (handleMessage)
                {
                    MessageEvents._context.Post(delegate (object state)
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