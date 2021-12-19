using System;
using System.Runtime.InteropServices;
using System.Text;

namespace UsbNotify
{
    public static class UsbNotification
    {
        public const int DbtDevicearrival = 0x8000; // system detected a new device        
        public const int DbtDeviceremovecomplete = 0x8004; // device is gone      
        public const int WmDevicechange = 0x0219; // device change event      
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

        public static void RegisterUsbDeviceNotification(params Guid[] deviceClasses)
        {
            foreach(var deviceClass in deviceClasses)
            {
                RegisterUsbDeviceNotification(MessageEvents.WindowHandle, deviceClass);
            }
            
            MessageEvents.WatchMessage(WmDevicechange);
            MessageEvents.MessageReceived += MessageEvents_MessageReceived;
        }


        private static void OnUsbDevicesChanged(string s)
        {
            if (UsbChanged != null)
                UsbChanged(s);
        }
        private static void MessageEvents_MessageReceived(System.Windows.Forms.Message m)
        {
            try
            {
                if ((WndMessage)m.Msg != WndMessage.WM_DEVICECHANGE)
                    return;

                if (m.LParam == IntPtr.Zero)
                {
                    OnUsbDevicesChanged("--");
                    return;
                }

                var dbh = (DEV_BROADCAST_HDR)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_HDR));

                switch ((WM_DEVICECHANGE)dbh.dbch_devicetype)
                {

                    case WM_DEVICECHANGE.DBT_DEVTYP_PORT:
                        {
                            var bytes = new byte[dbh.dbch_size - (int)WM_DEVICECHANGE.SIZE_OF_DBH];
                            Marshal.Copy(m.LParam + (int)WM_DEVICECHANGE.SIZE_OF_DBH, bytes, 0, bytes.Length);
                            var name = MarshalString<DEV_BROADCAST_HDR>(dbh.dbch_size, m.LParam);
                            OnUsbDevicesChanged(name);
                        }
                        break;

                    case WM_DEVICECHANGE.DBT_DEVTYP_DEVICEINTERFACE:
                        {
                            var xx = (DEV_BROADCAST_DEVICEINTERFACE)Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_DEVICEINTERFACE));
                            string name = "";

                            var action = "bloopy";

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

                            name = MarshalString<DEV_BROADCAST_DEVICEINTERFACE>(dbh.dbch_size, m.LParam);
                            OnUsbDevicesChanged($"Size - {xx.dbch_size} Name - {name} - Type :{xx.dbch_devicetype} {xx.dbcc_classguid} {action}");
                        }
                        break;
                    case WM_DEVICECHANGE.DBT_DEVTYP_OEM:
                        OnUsbDevicesChanged($"OEM 0X{dbh.dbch_devicetype.ToString("X8")}");
                        break;
                    case WM_DEVICECHANGE.DBT_DEVTYP_VOLUME:
                        OnUsbDevicesChanged($"VOLUME 0X{dbh.dbch_devicetype.ToString("X8")}");
                        break;

                    default:
                        OnUsbDevicesChanged($"wot! 0X{dbh.dbch_devicetype.ToString("X8")}");
                        break;
                }
            }catch(Exception ex)
            {
                Console.WriteLine($"Opps something went wrong with a usb even. \n{ex}");
            }
        }

        private static string MarshalString<T>(uint messageSize, IntPtr lparam)
        {
            int sizeofStruct = Marshal.SizeOf<T>();
            var size = messageSize - sizeofStruct;
            
            if (size < 0)
                throw new ArgumentOutOfRangeException($"Message is is wack sizeofStruct:{sizeofStruct}, messageSize{messageSize}");

            if(size > 2048)
                throw new ArgumentOutOfRangeException($"Message is is wack sizeofStruct:{sizeofStruct}, messageSize{messageSize}");
            
            var bytes = new byte[size];

            Marshal.Copy(lparam + sizeofStruct - sizeof(short) /*This is the string pointer itself*/, bytes, 0, bytes.Length);
            StringBuilder sb = new StringBuilder();
            foreach(var b in bytes) 
            {
                if (b == 0)
                    break;
                sb.Append(Convert.ToChar(b));
            }

            return sb.ToString();
        }

        public enum WndMessage
        {
            WM_DEVICECHANGE = 0x0219, // device change event   
        }

        public enum WM_DEVICECHANGE
        {
            // full list: https://docs.microsoft.com/en-us/windows/win32/devio/wm-devicechange
            DBT_DEVICEARRIVAL = 0x8000,             // A device or piece of media has been inserted and is now available.
            DBT_DEVICEREMOVECOMPLETE = 0x8004,      // A device or piece of media has been removed.

            DBT_DEVTYP_DEVICEINTERFACE = 0x00000005,    // Class of devices. This structure is a DEV_BROADCAST_DEVICEINTERFACE structure.
            DBT_DEVTYP_HANDLE = 0x00000006,             // File system handle. This structure is a DEV_BROADCAST_HANDLE structure.
            DBT_DEVTYP_OEM = 0x00000000,                // OEM- or IHV-defined device type. This structure is a DEV_BROADCAST_OEM structure.
            DBT_DEVTYP_PORT = 0x00000003,               // Port device (serial or parallel). This structure is a DEV_BROADCAST_PORT structure.
            DBT_DEVTYP_VOLUME = 0x00000002,             // Logical volume. This structure is a DEV_BROADCAST_VOLUME structure.

            SIZE_OF_DBH = 12,   // sizeof(DEV_BROADCAST_HDR)
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DEV_BROADCAST_HDR
        {
            internal UInt32 dbch_size;
            internal UInt32 dbch_devicetype;
            internal UInt32 dbch_reserved;
        };
      

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct DEV_BROADCAST_OEM
        {
            internal UInt32 dbch_size;
            internal UInt32 dbch_devicetype;
            internal UInt32 dbch_reserved;            
            internal uint dbco_identifier;
            internal uint dbco_suppfunc;
        };

       
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

            //notificationHandle = RegisterDeviceNotification(windowHandle, filter, DEVICE_NOTIFY_GUID);
            notificationHandle = RegisterDeviceNotificationW(windowHandle, filter, DEVICE_NOTIFY_GUID);
        }

        const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 0x00000004;
        const int DEVICE_NOTIFY_GUID = 0x00000000;

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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DEV_BROADCAST_DEVICEINTERFACE
        {
            internal int dbch_size;
            internal int dbch_devicetype;
            internal int dbch_reserved;
            internal Guid dbcc_classguid;
            internal short Name;
        };    
    }
}