namespace UsbNotify
{
    public static partial class UsbNotification
    {
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
    }
}