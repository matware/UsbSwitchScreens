using System.Collections.Generic;
using UsbNotify;

namespace MonitorSwitcher
{
    public class Settings
    {
        public string DefaultProfile { get; set; }

        /// <summary>
        /// When this is active, and the pc is shutdown, the shutdown this monitor
        /// </summary>
        public bool PowerOffOnShutdown { get; set; } = false;
        public Dictionary<string,List<MonitorSetting>> Profiles{get;set;}      

        public List<MonitorSetting> GetDefaultProfile()
        {
            return Profiles[DefaultProfile];
        }
    }

    public class MonitorSetting
    {
        public string MonitorName { get; set; } = null!;
        public InputSource InputId { get; set; }
        
        
    }
}