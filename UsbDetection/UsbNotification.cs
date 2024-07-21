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