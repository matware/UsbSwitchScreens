using System.Collections.Generic;

namespace MonitorSwitcher
{
    public class Settings
    {
        public List<MonitorSetting> Monitors{get;set;}      
    }

    public class MonitorSetting
    {
        public string MonitorName { get; set; } = null!;
        public uint InputId { get; set; }
    }
}