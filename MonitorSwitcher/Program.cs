using UsbNotify;
using Microsoft.Extensions.Configuration;
using System.Windows.Forms;
using System.Text;
using System;
using System.Collections.Generic;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;

namespace MonitorSwitcher
{
    class Program
    {
        static Settings settings;
        static MonitorSwitcher switcher = new MonitorSwitcher();
        static ConsoleHelper consoleHelper = new ConsoleHelper();
        static ConsoleTaskBar taskBarMenu;
        static void Main(string[] args)
        {
            ReadSettings();

            taskBarMenu = new ConsoleTaskBar();
            var switchProfiles = new Dictionary<string, Action>();
            
            foreach(var profileName in settings.Profiles.Keys)
            {
                switchProfiles[profileName] = () => { switcher.SwitchTo(settings.Profiles[profileName]); };
            }

            taskBarMenu.Init(switchProfiles, ()=> { switcher.PowerOff(settings.GetDefaultProfile()); });
            
            switcher.Init();

            consoleHelper.WriteStatus("\nMonitor Status\n");
            consoleHelper.WriteStatus(switcher.ToString());

            UsbNotification.RegisterUsbDeviceNotification(UsbNotification.KeyboardDeviceInterface);
            UsbNotification.KeyboardConnected += UsbNotification_KeyboardConnected;
            UsbNotification.KeyboardDisconnected += UsbNotification_KeyboardDisconnected;
            MessageEvents.WatchMessage((int)WndMessage.WM_ENDSESSION);
            MessageEvents.MessageReceived += MessageEvents_MessageReceived;
            MessageEvents.FormClosing += MessageEvents_ShutdownRequested;
            taskBarMenu.HideConsole();
            Application.Run();
        }

        private static void MessageEvents_ShutdownRequested()
        {

            if (Debugger.IsAttached || taskBarMenu.ExitRequested)
            {
                File.WriteAllText("form_closing.txt", $"{DateTime.Now}");
                return;
            }

            switcher.PowerOff(settings.GetDefaultProfile());
        }

        private static void MessageEvents_MessageReceived(Message msg)
        {
            if ((WndMessage)msg.Msg != WndMessage.WM_ENDSESSION)
                return;

            if (Debugger.IsAttached)
            {
                File.WriteAllText("windows_shutdown.txt", $"{DateTime.Now}");
                return;
            }

            switcher.PowerOff(settings.GetDefaultProfile());
        }


        private static void ReadSettings()
        {
            IConfiguration config = new ConfigurationBuilder()
                                    .AddJsonFile("appsettings.json")
                                    .AddEnvironmentVariables()
                                    .Build();

            settings = config.GetRequiredSection("Settings").Get<Settings>();

            if (settings.PowerOffOnShutdown)
            {
                SystemEvents.SessionEnding += SystemEvents_SessionEnding;
            }
            PrintSettings();
        }

        private static async void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            if (e.Reason != SessionEndReasons.SystemShutdown)
                return;
            switcher.PowerOff(settings.GetDefaultProfile());
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
