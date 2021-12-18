using UsbNotify;
using Microsoft.Extensions.Configuration;
using System.Windows.Forms;
using System.Text;

namespace MonitorSwitcher
{
    class Program
    {
        static Settings settings;
        static MonitorSwitcher switcher;
        static ConsoleHelper consoleHelper;
        static void Main(string[] args)
        {
            consoleHelper = new ConsoleHelper();
            var consoleTaskBark = new ConsoleTaskBar();
            consoleTaskBark.Init();

            ReadSettings();
            
            switcher = new MonitorSwitcher();
            
            switcher.Init();

            consoleHelper.WriteStatus("\nMonitor Status\n");
            consoleHelper.WriteStatus(switcher.ToString());

            UsbNotification.RegisterUsbDeviceNotification(UsbNotification.KeyboardDeviceInterface);
            UsbNotification.KeyboardConnected += UsbNotification_KeyboardConnected;
            UsbNotification.KeyboardDisconnected += UsbNotification_KeyboardDisconnected;

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
            sb.AppendLine("Settings");
            foreach (var s in settings.Monitors)
                sb.AppendLine($"\t{s.MonitorName}:{s.InputId}");

            consoleHelper.WriteInfo(sb.ToString());
        }

        private static void UsbNotification_KeyboardDisconnected(string obj)
        {
            consoleHelper.WriteInfo("Keyboard Disconnected\n");
        }

        private static void UsbNotification_KeyboardConnected(string obj)
        {
            consoleHelper.WriteInfo("Keyboard Connected\n");
            switcher.SwitchTo(settings.Monitors);
        }
    }
}
