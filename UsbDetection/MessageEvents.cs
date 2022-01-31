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

        private class MessageWindow : Form
        {
            private ReaderWriterLock _lock = new ReaderWriterLock();
            private Dictionary<int, bool> _messageSet =
                new Dictionary<int, bool>();

            public void RegisterEventForMessage(int messageID)
            {
                _lock.AcquireWriterLock(Timeout.Infinite);
                _messageSet[messageID] = true;
                _lock.ReleaseWriterLock();
            }

            protected override void WndProc(ref Message m)
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                bool handleMessage = _messageSet.ContainsKey(m.Msg);
                _lock.ReleaseReaderLock();

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