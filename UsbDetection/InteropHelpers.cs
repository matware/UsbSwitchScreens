using System;
using System.Runtime.InteropServices;
using System.Text;

namespace UsbNotify
{
    public static class InteropHelpers
    {
        public static string MarshalString<T>(this IntPtr lparam, uint messageSize)
        {
            int sizeofStruct = Marshal.SizeOf<T>();
            var size = messageSize - sizeofStruct;

            if (size < 0 || size > 2048)
                throw new ArgumentOutOfRangeException($"Message is wack sizeofStruct:{sizeofStruct}, messageSize{messageSize}");

            var bytes = new byte[size];
            IntPtr str_ptr = lparam + sizeofStruct - sizeof(short);
            
            return Marshal.PtrToStringAuto(str_ptr);
        }
    }
}