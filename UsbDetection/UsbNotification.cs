using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace UsbNotify
{
    public static partial class UsbNotification
    {
        public const int DbtDeviceArrival = 0x8000; // system detected a new device        
        public const int DbtDeviceRemoveComplete = 0x8004; // device is gone      
        const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 0x00000004;
        const int DEVICE_NOTIFY_GUID = 0x00000000; 
        private static readonly Guid GuidDevinterfaceUSBDevice = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED"); // USB devices
        private const int DBT_DEVTYP_DEVICEINTERFACE = 5;
        public static readonly Guid GUID_DEVINTERFACE_USB_DEVICE = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED"); // USB devices
        public static readonly Guid KeyboardDeviceInterface = new Guid("884b96c3-56ef-11d1-bc8c-00a0c91405dd"); // Keyboard
        public static readonly Guid MouseDeviceInterface = new Guid("378DE44C-56EF-11D1-BC8C-00A0C91405DD");
        public static readonly Guid GUID_DEVINTERFACE_HID = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");
        private static IntPtr notificationHandle;

        public static event Action<string> UsbChanged;

        public static event Action<string> KeyboardConnected;
        public static event Action<string> KeyboardDisconnected;

        public static void RegisterUsbDeviceNotification()
        {
            RegisterUsbDeviceNotification(
                //GUID_DEVINTERFACE_USB_DEVICE,
                KeyboardDeviceInterface
                //MouseDeviceInterface
                //GUID_DEVINTERFACE_HID
                );
        }

        public static void Log(string s)
        {
            Console.WriteLine($"Log {DateTime.Now}:{s}");
        }

        public static void Trace(string s)
        {
            if (!Debugger.IsAttached)
                return;

            Console.WriteLine($"Trace {DateTime.Now}:{s}");
        }

        public static void RegisterUsbDeviceNotification(params Guid[] deviceClasses)
        {
            foreach (var deviceClass in deviceClasses)
            {
                RegisterUsbDeviceNotification(MessageEvents.WindowHandle, deviceClass);
            }


            MessageEvents.DeviceConnected += MessageEvents_DeviceConnected; ;
        }

        private static void MessageEvents_DeviceConnected(UsbConnectionEventData deviceConnectedData)
        {
            if (deviceConnectedData == null)
                return;

            if (deviceConnectedData.Event != UsbEvent.Connected)
                return;

            KeyboardConnected(deviceConnectedData.VID);
        }

        private static void OnUsbDevicesChanged(string s)
        {
            if (UsbChanged != null)
                UsbChanged(s);
        }

        private static bool IsConnectionMessage(System.Windows.Forms.Message m)
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
                Trace("Message WM_DEVICECHANGE - Unknown device");
                OnUsbDevicesChanged("--");
                return false;
            }
            return true;
        }

    

        public static void GetDeviceChangeMessage(System.Windows.Forms.Message m)
        {
            try
            {
                if (!IsConnectionMessage(m)) return;
                var dbh = Marshal.PtrToStructure<DEV_BROADCAST_HDR>(m.LParam);

                switch ((WM_DEVICECHANGE)dbh.dbch_devicetype)
                {

                    case WM_DEVICECHANGE.DBT_DEVTYP_PORT:
                        {
                            Trace("Message WM_DEVICECHANGE - DBT_DEVTYP_PORT");
                            var bytes = new byte[dbh.dbch_size - (int)WM_DEVICECHANGE.SIZE_OF_DBH];
                            Marshal.Copy(m.LParam + (int)WM_DEVICECHANGE.SIZE_OF_DBH, bytes, 0, bytes.Length);
                            var name = m.LParam.MarshalString<DEV_BROADCAST_HDR>(dbh.dbch_size);
                            OnUsbDevicesChanged(name);
                        }
                        break;

                    case WM_DEVICECHANGE.DBT_DEVTYP_DEVICEINTERFACE:
                        {
                            Trace("Message WM_DEVICECHANGE - DBT_DEVTYP_DEVICEINTERFACE");
                            var deviceClass = Marshal.PtrToStructure<DEV_BROADCAST_DEVICEINTERFACE>(m.LParam);
                            string name = "";

                            var action = "unkown";

                            Trace($"Message DBT_DEVTYP_DEVICEINTERFACE - {dbh.dbch_devicetype.ToString("X8")}");

                            if ((int)WM_DEVICECHANGE.DBT_DEVICEARRIVAL == (int)m.WParam)
                            {
                                action = "connected";
                                if (KeyboardConnected != null)
                                    KeyboardConnected(name);
                            }

                            if ((int)WM_DEVICECHANGE.DBT_DEVICEREMOVECOMPLETE == (int)m.WParam)
                            {
                                action = "removed";
                                if (KeyboardDisconnected != null)
                                    KeyboardDisconnected(name);
                            }

                            name = m.LParam.MarshalString<DEV_BROADCAST_DEVICEINTERFACE>(dbh.dbch_size);
                            OnUsbDevicesChanged($"Size - {deviceClass.dbch_size} Name - {name} - Type :{deviceClass.dbch_devicetype} {deviceClass.dbcc_classguid} {action}");
                        }
                        break;

                    case WM_DEVICECHANGE.DBT_DEVTYP_OEM:
                        Trace("Message WM_DEVICECHANGE - DBT_DEVTYP_OEM");
                        OnUsbDevicesChanged($"OEM 0X{dbh.dbch_devicetype.ToString("X8")}");
                        break;

                    case WM_DEVICECHANGE.DBT_DEVTYP_VOLUME:
                        Trace("Message WM_DEVICECHANGE - DBT_DEVTYP_VOLUME");
                        OnUsbDevicesChanged($"VOLUME 0X{dbh.dbch_devicetype.ToString("X8")}");
                        break;

                    default:
                        Trace($"Message WM_DEVICECHANGE - {dbh.dbch_devicetype.ToString("X8")}");
                        OnUsbDevicesChanged($"wot! 0X{dbh.dbch_devicetype.ToString("X8")}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Opps something went wrong with a usb even. \n{ex}");
            }
        }


        /// <summary>
        /// Registers a window to receive notifications when USB devices are plugged or unplugged.
        /// </summary>
        /// <param name="windowHandle">Handle to the window receiving notifications.</param>
        public static void RegisterUsbDeviceNotification(IntPtr windowHandle, Guid guid)
        {
            DEV_BROADCAST_DEVICEINTERFACE dbi = new DEV_BROADCAST_DEVICEINTERFACE
            {
                dbch_devicetype = DBT_DEVTYP_DEVICEINTERFACE,
                dbch_reserved = 0,
                dbcc_classguid = guid,
                Name = 0
            };

            dbi.dbch_size = Marshal.SizeOf(dbi);
            IntPtr filter = Marshal.AllocHGlobal(dbi.dbch_size);
            Marshal.StructureToPtr(dbi, filter, true);

            notificationHandle = RegisterDeviceNotificationW(windowHandle, filter, DEVICE_NOTIFY_GUID);
        }

        /// <summary>
        /// Unregisters the window for USB device notifications
        /// </summary>
        public static void UnregisterUsbDeviceNotification()
        {
            UnregisterDeviceNotification(notificationHandle);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr notificationFilter, int flags);

        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotificationA(IntPtr recipient, IntPtr notificationFilter, int flags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotificationW(IntPtr recipient, IntPtr notificationFilter, int flags);

        [DllImport("user32.dll")]
        private static extern bool UnregisterDeviceNotification(IntPtr handle);
    }
}