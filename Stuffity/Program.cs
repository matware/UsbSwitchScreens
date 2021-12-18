using UsbNotify;
using Microsoft.Extensions.Configuration;
using System.Windows.Forms;
using System.Text;
using System;
using System.Collections.Generic;

namespace MonitorSwitcher
{
    class Program
    {
        static Settings settings;
        static MonitorSwitcher switcher = new MonitorSwitcher();
        static ConsoleHelper consoleHelper = new ConsoleHelper();
        static void Main(string[] args)
        {
            ReadSettings();

            var taskBarMenu = new ConsoleTaskBar();
            var switchProfiles = new Dictionary<string, Action>();
            
            foreach(var profileName in settings.Profiles.Keys)
            {
                switchProfiles[profileName] = () => { switcher.SwitchTo(settings.Profiles[profileName]); };
            }

            taskBarMenu.Init(switchProfiles);
            
            switcher.Init();

            consoleHelper.WriteStatus("\nMonitor Status\n");
            consoleHelper.WriteStatus(switcher.ToString());

            UsbNotification.RegisterUsbDeviceNotification(UsbNotification.KeyboardDeviceInterface);
            UsbNotification.KeyboardConnected += UsbNotification_KeyboardConnected;
            UsbNotification.KeyboardDisconnected += UsbNotification_KeyboardDisconnected;
            taskBarMenu.HideConsole();
            Application.Run();
        }

        private static void ReadSettings()
        {
            IConfiguration config = new ConfigurationBuilder()
                                    .AddJsonFile("appsettings.json")
                                    .AddEnvironmentVariables()
                                    .Build();

            settings = config.GetRequiredSection("Settings").Get<Settings>();

            PrintSettings();
        }


        public static void PrintSettings()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Settings:");
            sb.AppendLine($"\tDefault Profile {settings.DefaultProfile}");
            foreach (var s in settings.Profiles)
            {
                sb.AppendLine($"\t {s.Key}");
                foreach(var m in s.Value)
                sb.AppendLine($"\t\t{m.MonitorName}:{m.InputId}");
            }

            consoleHelper.WriteInfo(sb.ToString());
        }

        private static void UsbNotification_KeyboardDisconnected(string obj)
        {
            consoleHelper.WriteInfo("Keyboard Disconnected\n");
        }

        private static void UsbNotification_KeyboardConnected(string obj)
        {
            consoleHelper.WriteInfo("Keyboard Connected\n");
            switcher.SwitchTo(settings.GetDefaultProfile());
        }
    }
}
