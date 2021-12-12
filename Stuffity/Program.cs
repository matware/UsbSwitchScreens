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
            //UsbNotification.Boop += UsbNotification_Boop;
            UsbNotification.KeyboardConnected += UsbNotification_KeyboardConnected;
            UsbNotification.KeyboardDisconnected += UsbNotification_KeyboardDisconnected;
            do
            {
                //var usbDevices = GetUSBDevices();
                //foreach (var usbDevice in usbDevices)
                //{
                //    Console.WriteLine("Device ID: {0}, PNP Device ID: {1}, Description: {2}",
                //        usbDevice.DeviceID, usbDevice.PnpDeviceID, usbDevice.Description);
                //}

                Thread.Sleep(100);
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
