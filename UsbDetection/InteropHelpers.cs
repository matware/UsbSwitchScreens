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

            Marshal.Copy(lparam + sizeofStruct - sizeof(short) /*This is the string pointer itself*/, bytes, 0, bytes.Length);
            StringBuilder sb = new StringBuilder();
            foreach (var b in bytes)
            {
                if (b == 0)
                    break;
                sb.Append(Convert.ToChar(b));
            }

            return sb.ToString();
        }
    }
}