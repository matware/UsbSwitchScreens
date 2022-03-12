using System;

namespace UsbNotify
{
    /*
    Byte: SL Input Definition
01h Analog video(R/G/B) 1
02h Analog video(R/G/B) 2
03h Digital video(TMDS) 1 DVI 1
04h Digital video(TMDS) 2 DVI 2
05h Composite video 1
06h Composite video 2
07h S-video 1
08h S-video 2
09h Tuner 1
0Ah Tuner 2
0Bh Tuner 3
0Ch Component video(YPbPr / YCbCr) 1
0Dh Component video(YPbPr / YCbCr) 2
0Eh Component video(YPbPr / YCbCr) 3
0Fh DisplayPort 1
10h DisplayPort 2
11h Digital Video(TMDS) 3 HDMI 1
12h Digital Video(TMDS) 4 HDMI 2
    */

    [Flags]
    public enum InputSource
    {
        ANALOG_VIDEO1 = 0x01,
        ANALOG_VIDEO2 = 0x02,
        DVI1 = 0x03,
        DVI2 = 0x04,
        COMPOSITE1 = 0x05,
        COMPOSITE2 = 0x06,
        SVIDEO1 = 0x07,
        SVIDEO2 = 0x08,
        COMPOSITE1_YprPb = 0x0c,
        COMPOSITE2_YprPb = 0x0d,

        DISPLAY_PORT1 = 0x0F,
        DISPLAY_PORT2 = 0x10,
        HDMI1 = 0x11,
        HDMI2 = 0x12,
    }
}