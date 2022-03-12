using System;

namespace UsbNotify
{
    /// <summary>
    /// 01: DPM: On,  DPMS: Off
    /// 04: DPM: Off, DPMS: Off
    /// 05: Write only value to turn off display
    /// </summary>
    [Flags]
    public enum PowerMode
    {
        DPM_ON = 0x01,
        DPM_OFF = 0x04,
        OFF = 0x05,
    }
}