using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
//using System.Threading;
using UsbNotify;
using Microsoft.Extensions.Configuration;

namespace Stuffity
{
    class Program
    {
        static Settings settings;
        static CapabilitiesLogic cap;
         static MonitorLogic monLogic;
         static VCPFeatureLogic vcpLogic;
        public static Dictionary<string, Monitor> monitors = new Dictionary<string, Monitor>();
        static void Main(string[] args)
        {

            IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
            settings = config.GetRequiredSection("Settings").Get<Settings>();

            foreach (var s in settings.Monitors)
                Console.WriteLine($"KeyOne = {s.MonitorName} {s.InputId}");

            UsbNotification.RegisterUsbDeviceNotification(UsbNotification.KeyboardDeviceInterface);
            UsbNotification.KeyboardConnected += UsbNotification_KeyboardConnected;
            UsbNotification.KeyboardDisconnected += UsbNotification_KeyboardDisconnected;

            InitVcp();

            var mons = monLogic.GetAll();

            using IHost host = Host.CreateDefaultBuilder(args).Build();
            foreach (var mon in mons)
            {
                monitors[mon.Model] = mon;
            }

            do
            {
                System.Threading.Thread.Sleep(2500);
            } while (true);


            Console.Read();
        }

        private static void InitVcp()
        {
            cap = new CapabilitiesLogic();

            monLogic = new MonitorLogic(cap);
            vcpLogic = new VCPFeatureLogic();
        }

        static DateTime startListeningAgain = DateTime.Now;
        
        static private bool SkipForAFewSecs()
        {
            var now = DateTime.Now;
            if (now > startListeningAgain)
            {
                startListeningAgain = startListeningAgain.AddSeconds(5);
                return false;
            }
            return true;

        }


        private static void PrintMonitorStatus()
        {
            foreach (var mon in monitors.Values)
            {
                var currentInput = new VCPFeatureModel(VCPFeature.INPUT_SOURCE, mon, vcpLogic, cap);
                Console.WriteLine(mon.Model + $" Current Input = {currentInput.CurrentValue} ||  {currentInput.CurrentValue & 0x1f} Max = {currentInput.MaximumValue}");

                foreach (var source in mon.InputSources)
                {
                    var maskedSource = source & 0x1f;
                    var sourceModel = new InputSourceModel(maskedSource, mon, vcpLogic);
                    
                    Console.Write($"{sourceModel.Name} {maskedSource}");

                    if ((currentInput.CurrentValue & 0x1f) == source)
                        Console.WriteLine("*");
                    else
                        Console.WriteLine();
                }
            }

            
        }

        private static void UsbNotification_KeyboardDisconnected(string obj)
        {
            Console.WriteLine("Disconnected");
        }

        

        private static void UsbNotification_KeyboardConnected(string obj)
        {
            Console.WriteLine("Keyboard Connected");
            
            if (SkipForAFewSecs())
                return;

            foreach (var s in settings.Monitors)
            {
                Monitor mon;
                if (!monitors.TryGetValue(s.MonitorName, out mon))
                {
                    Console.WriteLine($"oopsy, couldn't find {s.MonitorName}");
                    continue;
                }
                var selectedSource = new InputSourceModel(s.InputId, mon, vcpLogic);
                selectedSource.SetThisAsInputSource();
            }
        }

        private static void UsbNotification_Boop(string obj)
        {
            Console.WriteLine($"Boop {obj}");
        }       

        static List<USBDeviceInfo> GetUSBDevices()
        {
            List<USBDeviceInfo> devices = new List<USBDeviceInfo>();

            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_Keyboard"))
            using (var collection = searcher.Get())
            {

                foreach (var device in collection)
                {
                    devices.Add(new USBDeviceInfo(
                        (string)device.GetPropertyValue("DeviceID"),
                        (string)device.GetPropertyValue("PNPDeviceID"),
                        (string)device.GetPropertyValue("Description")
                    ));

                   // Console.WriteLine(device.GetPropertyValue("Name"));
                }

            }
            return devices;
        }
    }
}
