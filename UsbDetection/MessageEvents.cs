using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using static UsbNotify.UsbNotification;

namespace UsbNotify
{
    public static class MessageEvents
    {
        private static SynchronizationContext context;
        private static event Action<UsbConnectionEventData> deviceConnected;
        public static event Action<UsbConnectionEventData> DeviceConnected { add
            {
                WatchMessage((int)WndMessage.WM_DEVICECHANGE);
                deviceConnected += value;
            }
            remove
            {
                deviceConnected -= value;
            }
        }

        private static event Action shutdown;
        public static event Action Shutdown { add {
                
                WatchMessage((int)WndMessage.WM_ENDSESSION);
                shutdown += value; } remove { shutdown -= value; } }
        public static event Action FormClosing;

        private static MessageWindow window;
        private static IntPtr windowHandle;

        private static void WatchMessage(int message)
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

            public const int DbtDeviceArrival = 0x8000; // system detected a new device        
            public const int DbtDeviceRemoveComplete = 0x8004; // device is gone      

            private static bool IsConnectionMessage(Message m)
            {
                if ((WndMessage)m.Msg != WndMessage.WM_DEVICECHANGE)
                    return false;

                switch (m.WParam)
                {
                    case DbtDeviceArrival:
                    case DbtDeviceRemoveComplete:
                        break;
                    default:
                        return false;
                }


                if (m.LParam == IntPtr.Zero)
                {
                    return false;
                }

                return true;
            }
            
            
            private static UsbConnectionEventData GetDeviceChangeMessage(Message m)
            {
                try
                {
                    if (!IsConnectionMessage(m)) return null;

                    var dbh = Marshal.PtrToStructure<DEV_BROADCAST_HDR>(m.LParam);

                    switch ((WM_DEVICECHANGE)dbh.dbch_devicetype)
                    {
                        case WM_DEVICECHANGE.DBT_DEVTYP_DEVICEINTERFACE:
                            {
                                var deviceClass = Marshal.PtrToStructure<DEV_BROADCAST_DEVICEINTERFACE>(m.LParam);
                                var name = m.LParam.MarshalString<DEV_BROADCAST_DEVICEINTERFACE>(dbh.dbch_size);
                                
                                if ((int)WM_DEVICECHANGE.DBT_DEVICEARRIVAL == (int)m.WParam)
                                {
                                    return new UsbConnectionEventData(name, UsbEvent.Connected);
                                    
                                }

                                if ((int)WM_DEVICECHANGE.DBT_DEVICEREMOVECOMPLETE == (int)m.WParam)
                                {
                                    return new UsbConnectionEventData(name, UsbEvent.Disconnected);
                                }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Opps something went wrong with a usb even. \n{ex}");
                }

                return null;
                     
            }

            protected override void WndProc(ref Message m)
            {
                rwLock.AcquireReaderLock(Timeout.Infinite);
                bool handleMessage = messageSet.Contains(m.Msg);
                rwLock.ReleaseReaderLock();

                try
                {
                    if (handleMessage)
                    {
                        Action? a = null;
                        switch ((WndMessage)m.Msg)
                        {
                            case WndMessage.WM_ENDSESSION:
                                a = MessageEvents.shutdown;
                                shutdown?.Invoke();
                                break;
                            case WndMessage.WM_DEVICECHANGE:

                                if (IsConnectionMessage(m))
                                {
                                    var deviceConnectedData = GetDeviceChangeMessage(m);
                                    if (deviceConnectedData != null && deviceConnectedData.Event == UsbEvent.Connected)
                                    {
                                        var ev = MessageEvents.deviceConnected;
                                        a = () =>
                                        {
                                            Console.WriteLine(deviceConnectedData.Event);
                                            ev?.Invoke(deviceConnectedData);
                                        };
                                    }
                                }

                                break;
                        }
                        if (a != null)
                        {
                            MessageEvents.context.Post(delegate (object state)
                            {
                                var action = (Action?)state;
                                action?.Invoke();
                            }, a);
                        }
                    }
                }
                finally
                {
                    base.WndProc(ref m);
                }
            }
        }
    }

    public class UsbConnectionEventData
    {
        private readonly static Regex VIDRegex = new Regex(".*(?<VID>VID_+\\d+).*(?<PID>PID_+\\d+).*", RegexOptions.Compiled);
        public UsbConnectionEventData(string name, UsbEvent ev)
        {
            Event = ev;
            var match = VIDRegex.Match(name);
            if (match.Success)
            {
                VID = match.Groups["VID"].Value;
                PID = match.Groups["PID"].Value;
            }
        }
        public string VID { get; private set; }
        public string PID { get; private set; }
        public UsbEvent Event { get; private set; }
    }

    public enum UsbEvent
    {
        Connected,
        Disconnected,
        Unkown
    }

}