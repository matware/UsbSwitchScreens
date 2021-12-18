using System.Collections.Generic;

namespace MonitorSwitcher
{
    public class Settings
    {
        public string DefaultProfile { get; set; }
        public Dictionary<string,List<MonitorSetting>> Profiles{get;set;}      

        public List<MonitorSetting> GetDefaultProfile()
        {
            return Profiles[DefaultProfile];
        }
    }

    public class MonitorSetting
    {
        public string MonitorName { get; set; } = null!;
        public uint InputId { get; set; }
    }
}