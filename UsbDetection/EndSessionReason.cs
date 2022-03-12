namespace UsbNotify
{
    public enum EndSessionReason : uint
    {
        ENDSESSION_CLOSEAPP = 0x0001,
        ENDSESSION_CRITICAL = 0x40000000,
        ENDSESSION_LOGOFF = 0x80000000
    }
}