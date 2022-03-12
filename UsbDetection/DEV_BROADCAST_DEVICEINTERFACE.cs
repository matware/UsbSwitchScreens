using System;
using System.Runtime.InteropServices;

namespace UsbNotify
{
    public static partial class UsbNotification
    {
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