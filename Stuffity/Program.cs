using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading;
using UsbNotify;
namespace Stuffity
{
    class Program
    {

        static void Main(string[] args)
        {
            UsbNotification.RegisterUsbDeviceNotification(UsbNotification.KeyboardDeviceInterface);
            //UsbNotification.Boop += UsbNotification_Boop;
            UsbNotification.KeyboardConnected += UsbNotification_KeyboardConnected;
            UsbNotification.KeyboardDisconnected += UsbNotification_KeyboardDisconnected;

            var cap = new CapabilitiesLogic();

            var monLogic = new MonitorLogic(cap);
            var vcpLogic = new VCPFeatureLogic();
            var mons = monLogic.GetAll();
            
            foreach (var mon in mons)
            {
                var currentInput = new VCPFeatureModel(VCPFeature.INPUT_SOURCE, mon, vcpLogic, cap);
                Console.WriteLine(mon.Model +$" Current Input = {currentInput.CurrentValue} ||  {currentInput.CurrentValue & 0x1f} Max = {currentInput.MaximumValue}");
                
                foreach (var s in mon.InputSources)
                {
                    var ss = s & 0x1f;                    
                    var x = new InputSourceModel(ss, mon, vcpLogic);
                    Console.Write($"{x.Name} {ss}");
                    if ((currentInput.CurrentValue & 0x1f) == s)
                        Console.WriteLine("*");
                    else
                        Console.WriteLine();                    
                }
                var a = mon.InputSources.Last();
                
                var selectedSource = new InputSourceModel(a, mon, vcpLogic);
                selectedSource.SetThisAsInputSource();                
            }


            do
            {
                Thread.Sleep(2500);
            } while (true);


            Console.Read();
        }

        private static void UsbNotification_KeyboardDisconnected(string obj)
        {
            Console.WriteLine("Disconnected");
        }

        private static void UsbNotification_KeyboardConnected(string obj)
        {
            Console.WriteLine("Connected");
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
