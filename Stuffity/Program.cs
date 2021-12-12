using System;
using System.Collections.Generic;
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
            UsbNotification.Boop += UsbNotification_Boop;

            do
            {
                //var usbDevices = GetUSBDevices();
                //foreach (var usbDevice in usbDevices)
                //{
                //    Console.WriteLine("Device ID: {0}, PNP Device ID: {1}, Description: {2}",
                //        usbDevice.DeviceID, usbDevice.PnpDeviceID, usbDevice.Description);
                //}

                Thread.Sleep(2000);
            } while (true);


            Console.Read();
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
