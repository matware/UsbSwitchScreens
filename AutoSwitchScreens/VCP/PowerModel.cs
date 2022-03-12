using System;

namespace UsbNotify
{
    public class PowerModel
    {
        private VCPFeatureLogic vcpFeatureLogic;
        private Monitor monitor;

        public PowerModel(Monitor monitor, VCPFeatureLogic vcpFeatureLogic)
        {
            this.vcpFeatureLogic = vcpFeatureLogic;
            this.monitor = monitor;            
        }

        public bool Off()
        {
            if (vcpFeatureLogic.SetValue(monitor, VCPFeature.MONITORPOWER, (uint)PowerMode.OFF))
                return true;

            Console.Write($"Failed to switch off monitor {monitor}");
            return false;
        }
      
    }
}