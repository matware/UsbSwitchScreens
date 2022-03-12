using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace UsbNotify
{
    public class VCPFeatureValue
    {
        public uint CurrentValue { get; set; }
        public uint MaximumValue { get; set; }
    }

    public class VCPFeatureLogic
    {

        public VCPFeatureValue GetVCPFeature(Monitor monitor, VCPFeature vcpFeature)
        {
            uint pvct = 0;
            uint pdwCurrentValue = 0;
            uint pdwMaximumValue = 0;
            //bool result = GetVCPFeatureAndVCPFeatureReply(monitor.PhysicalMonitor.hPhysicalMonitor, (byte)vcpFeature, ref pvct, ref pdwCurrentValue, ref pdwMaximumValue);
            bool result = GetVCPFeature(monitor.PhysicalMonitor.hPhysicalMonitor, (byte)vcpFeature, ref pvct, ref pdwCurrentValue, ref pdwMaximumValue);
            //Bugfix for slow Monitors
            for (int i = 0; i < MonToolConfiguration.REQUEST_REPEATS && result == false; i++)
            {

                System.Threading.Thread.Sleep(MonToolConfiguration.REQUEST_TIMEOUT);
                result = GetVCPFeatureAndVCPFeatureReply(monitor.PhysicalMonitor.hPhysicalMonitor, (byte)vcpFeature, ref pvct, ref pdwCurrentValue, ref pdwMaximumValue);
            }            

            return new VCPFeatureValue { CurrentValue = pdwCurrentValue, MaximumValue = pdwMaximumValue };

        }

        public bool SetValue(Monitor monitor, VCPFeature vcpFeature, uint newValue)
        {
            bool result;
            int retry = 0;
            //For slow Monitors
            do
            {
                result = SetVCPFeature(monitor.PhysicalMonitor.hPhysicalMonitor, (byte)vcpFeature, (uint)newValue & 0x1f);
            } while (!result && retry++ < MonToolConfiguration.REQUEST_REPEATS);

            return result;
        }

        [DllImport("dxva2.dll", EntryPoint = "GetVCPFeatureAndVCPFeatureReply")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetVCPFeatureAndVCPFeatureReply(IntPtr handle, byte bVCPCode, ref uint pvct, ref uint pdwCurrentValue, ref uint pdwMaximumValue);

        [DllImport("dxva2.dll", EntryPoint = "SetVCPFeature")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetVCPFeature(IntPtr handle, byte bVCPCode, uint dwNewValue);

        [DllImport("gdi32.dll", EntryPoint = "DDCCIGetVCPFeature")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetVCPFeature(IntPtr handle, byte bVCPCode, ref uint pvct, ref uint pdwCurrentValue, ref uint pdwMaximumValue);


    }

    public class BaseModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public class VCPFeatureModel : BaseModel
    {
        private VCPFeature vcpFeature;
        private Monitor monitor;
        private VCPFeatureLogic vcpFeatureLogic;
        private ICapabilitiesLogic capabilitiesLogic;

        private bool isCapable;
        private VCPFeatureValue vcpFeatureValue = null;

        public VCPFeatureModel(VCPFeature vcpFeature, Monitor monitor, VCPFeatureLogic vcpFeatureLogic, ICapabilitiesLogic capabilitiesLogic)
        {
            this.vcpFeature = vcpFeature;
            this.monitor = monitor;
            this.vcpFeatureLogic = vcpFeatureLogic;
            this.capabilitiesLogic = capabilitiesLogic;

            isCapable = capabilitiesLogic.IsCapable(monitor, vcpFeature);
        }

        public uint CurrentValue
        {
            get
            {
                if (!isCapable)
                    return 0;

                if (vcpFeatureValue == null)
                    GetVcpFeatureValue();

                return vcpFeatureValue.CurrentValue;
            }

            set
            {
                if (isCapable)
                {
                    vcpFeatureValue.CurrentValue = value;

                    if (!vcpFeatureLogic.SetValue(monitor, vcpFeature, value))
                        GetVcpFeatureValue();
                
                    OnPropertyChanged(nameof(CurrentValue));
                }
            }
        }

        public uint MaximumValue
        {

            get
            {
                if (!isCapable)
                {
                    return 0;
                }

                if (vcpFeatureValue == null)
                {
                    GetVcpFeatureValue();
                }

                return vcpFeatureValue.MaximumValue;
            }

        }

        public bool IsCapable
        {
            get { return isCapable; }
        }


        public void Refresh()
        {
            GetVcpFeatureValue();
            OnPropertyChanged(nameof(CurrentValue));
        }

        public string Name
        {
            get
            {
                switch (vcpFeature)
                {
                    case VCPFeature.COLOR_PRESET:
                        return "Color Preset";
                    case VCPFeature.CONTRAST:
                        return "Contrast";
                    case VCPFeature.INPUT_SOURCE:
                        return "Input Source";
                    case VCPFeature.LUMINANCE:
                        return "Luminance";
                    case VCPFeature.RESTORE_FACTORY_COLOR_DEFAULTS:
                        return "Restore Color Defaults";
                    case VCPFeature.RESTORE_FACTORY_DEFAULTS:
                        return "Restore Factory Defaults";
                    case VCPFeature.RESTORE_FACTORY_LUMINANCE_DEFAULTS:
                        return "Restore Luminance Defaults";
                    case VCPFeature.SPEAKER_VOLUME:
                        return "Speaker Volume";
                    case VCPFeature.STORE_RESTORE_SETTINGS:
                        return "Store/Restore Settings";
                    case VCPFeature.VIDEO_BLACK_LEVEL_BLUE:
                        return "Blue Level";
                    case VCPFeature.VIDEO_BLACK_LEVEL_GREEN:
                        return "Green Level";
                    case VCPFeature.VIDEO_BLACK_LEVEL_RED:
                        return "Red Level";
                    case VCPFeature.VIDEO_GAIN_BLUE:
                        return "Gain Blue";
                    case VCPFeature.VIDEO_GAIN_GREEN:
                        return "Gain Green";
                    case VCPFeature.VIDEO_GAIN_RED:
                        return "Gain Red";
                    default:
                        return "Unknown";
                }

            }
        }

        private void GetVcpFeatureValue()
        {
            vcpFeatureValue = vcpFeatureLogic.GetVCPFeature(monitor, vcpFeature);
        }

    }
}