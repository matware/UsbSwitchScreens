using System;
using System.Collections.Generic;
using UsbNotify;
using System.Text;

namespace MonitorSwitcher
{
    public class MonitorSwitcher
    {
        private CapabilitiesLogic cap;
        private MonitorLogic monLogic;
        private VCPFeatureLogic vcpLogic;
        private DateTime startListeningAgain = DateTime.Now;
        private Dictionary<string, Monitor> monitors = new Dictionary<string, Monitor>();
        public MonitorSwitcher()
        {
            cap = new CapabilitiesLogic();
            monLogic = new MonitorLogic(cap);
            vcpLogic = new VCPFeatureLogic();
        }
        public void Init()
        {        
            var mons = monLogic.GetAll();

            foreach (var mon in mons)
                monitors[mon.Model] = mon;
        }

        private bool CheckMonitorsExist(IEnumerable<MonitorSetting> sources)
        {
            foreach (var s in sources)
            {
                if (!monitors.ContainsKey(s.MonitorName))
                {
                    Console.WriteLine($"oopsy, couldn't find {s.MonitorName}");
                    return false;
                }
            }
            return true;
        }

        public void PowerOff(IEnumerable<MonitorSetting> sources)
        {
            if (!CheckMonitorsExist(sources))
                Init();
            foreach (var s in sources)
            {
                Monitor mon;
                if (!monitors.TryGetValue(s.MonitorName, out mon))
                {
                    Console.WriteLine($"oopsy, couldn't find {s.MonitorName}");
                    continue;
                }

                var source = new PowerModel(mon, vcpLogic);

                if (source.Off())
                    continue;
            }
        }

        public void SwitchTo(IEnumerable<MonitorSetting> sources, int depth = 0)
        {
            if (SkipForAFewSecs())
                return;

            if (!CheckMonitorsExist(sources))
                Init();

            bool switchFailed = false;

            foreach (var s in sources)
            {
                Monitor mon;
                if (!monitors.TryGetValue(s.MonitorName, out mon))
                {
                    Console.WriteLine($"oopsy, couldn't find {s.MonitorName}");
                    continue;
                }

                var source = new InputSourceModel(s.InputId, mon, vcpLogic);

                if (source.Select())
                    continue;

                switchFailed = true;
            }

            if (!switchFailed)
                return;

            if (depth >= 2) // Don't recurse too far
                return;

            Init();
            SwitchTo(sources, ++depth);
        }

        private bool SkipForAFewSecs()
        {
            var now = DateTime.Now;
            if (now > startListeningAgain)
            {
                startListeningAgain = startListeningAgain.AddSeconds(5);
                return false;
            }
            return true;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var mon in monitors.Values)
            {
                var currentInput = new VCPFeatureModel(VCPFeature.INPUT_SOURCE, mon, vcpLogic, cap);
                sb.AppendLine(mon.Model + $" Current Input = {currentInput.CurrentValue} || {currentInput.CurrentValue & 0x1f} Max = {currentInput.MaximumValue}");

                foreach (var source in mon.InputSources)
                {
                    var sourceModel = new InputSourceModel(source, mon, vcpLogic);

                    var maskedSource = (uint)source & 0x1f;
                    sb.Append($"\t{maskedSource}\t{sourceModel.Name}");

                    if ((currentInput.CurrentValue & 0x1f) == maskedSource)
                        sb.AppendLine("*");
                    else
                        sb.AppendLine();
                }

                sb.AppendLine();
            }
            return sb.ToString();

        }
    }
}
