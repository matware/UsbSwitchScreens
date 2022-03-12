using System;

namespace UsbNotify
{
    public class InputSourceModel
    {
        private InputSource inputSource;
        private VCPFeatureLogic vcpFeatureLogic;
        private Monitor monitor;

        public InputSourceModel(InputSource inputSource, Monitor monitor, VCPFeatureLogic vcpFeatureLogic)
        {
            this.inputSource = inputSource;
            this.vcpFeatureLogic = vcpFeatureLogic;
            this.monitor = monitor;
        }

        public string Name
        {
            get
            {
                return ((InputSource)inputSource).ToString();
            }
        }

        public bool Select()
        {
            if(!vcpFeatureLogic.SetValue(monitor, VCPFeature.INPUT_SOURCE, (uint)inputSource))
            {
                Console.Write($"Failed to switch to source {inputSource} on monitor {monitor}");
                return false;
            }
            return true;
        }    
    }
}