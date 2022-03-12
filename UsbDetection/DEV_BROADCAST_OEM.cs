using System;
using System.Runtime.InteropServices;

namespace UsbNotify
{
    public static partial class UsbNotification
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct DEV_BROADCAST_OEM
        {
            internal UInt32 dbch_size;
            internal UInt32 dbch_devicetype;
            internal UInt32 dbch_reserved;            
            internal uint dbco_identifier;
            internal uint dbco_suppfunc;
        };
    }
}