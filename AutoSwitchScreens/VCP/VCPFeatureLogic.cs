using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Input;

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

        public bool SetVCPFeature(Monitor monitor, VCPFeature vcpFeature, uint newValue)
        {
            bool result = SetVCPFeature(monitor.PhysicalMonitor.hPhysicalMonitor, (byte)vcpFeature, newValue);

            //Bugfix for slow Monitors
            for (int i = 0; i < MonToolConfiguration.REQUEST_REPEATS && result == false; i++)
            {
                System.Threading.Thread.Sleep(MonToolConfiguration.REQUEST_TIMEOUT);
                result = SetVCPFeature(monitor.PhysicalMonitor.hPhysicalMonitor, (byte)vcpFeature, newValue);
            }
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

    public class ChangeInputSourceCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private InputSourceModel inputSourceModel;

        public ChangeInputSourceCommand(InputSourceModel inputSourceModel)

        {
            this.inputSourceModel = inputSourceModel;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            inputSourceModel.Select();
        }
    }

    public class InputSourceModel
    {

        private uint inputSource;
        private VCPFeatureLogic vcpFeatureLogic;
        private Monitor monitor;

        public InputSourceModel(uint inputSource, Monitor monitor, VCPFeatureLogic vcpFeatureLogic)
        {
            this.inputSource = inputSource & 0x1f;
            this.vcpFeatureLogic = vcpFeatureLogic;
            this.monitor = monitor;
            ChangeInputSourceCommand = new ChangeInputSourceCommand(this);
        }

        /*
        Byte: SL Input Definition
01h Analog video(R/G/B) 1
02h Analog video(R/G/B) 2
03h Digital video(TMDS) 1 DVI 1
04h Digital video(TMDS) 2 DVI 2
05h Composite video 1
06h Composite video 2
07h S-video 1
08h S-video 2
09h Tuner 1
0Ah Tuner 2
0Bh Tuner 3
0Ch Component video(YPbPr / YCbCr) 1
0Dh Component video(YPbPr / YCbCr) 2
0Eh Component video(YPbPr / YCbCr) 3
0Fh DisplayPort 1
10h DisplayPort 2
11h Digital Video(TMDS) 3 HDMI 1
12h Digital Video(TMDS) 4 HDMI 2
        */

        public string Name
        {
            get
            {
                return ((InputSource)inputSource).ToString();
            }
        }

        public ChangeInputSourceCommand ChangeInputSourceCommand { get; }

        public bool Select()
        {
            if(!vcpFeatureLogic.SetVCPFeature(monitor, VCPFeature.INPUT_SOURCE, inputSource))
            {
                Console.Write($"Failed to switch to source {inputSource} on monitor {monitor}");
                return false;
            }
            return true;
        }

    }


    [Flags]
    public enum InputSource
    {
        ANALOG_VIDEO1 = 0x01,
        ANALOG_VIDEO2 = 0x02,
        DVI1 = 0x03,
        DVI2 = 0x04,
        COMPOSITE1 = 0x05,
        COMPOSITE2 = 0x06,
        SVIDEO1 = 0x07,
        SVIDEO2 = 0x08,
        COMPOSITE1_YprPb = 0x0c,
        COMPOSITE2_YprPb = 0x0d,

        DISPLAY_PORT1 = 0x0F,
        DISPLAY_PORT2 = 0x10,
        HDMI1 = 0x11,
        HDMI2 = 0x12,
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
                {
                    return 0;
                }

                if (vcpFeatureValue == null)
                {
                    GetVcpFeatureValue();
                }

                return vcpFeatureValue.CurrentValue;
            }

            set
            {
                if (isCapable)
                {
                    vcpFeatureValue.CurrentValue = value;

                    if (!vcpFeatureLogic.SetVCPFeature(monitor, vcpFeature, value))
                    {
                        GetVcpFeatureValue();
                    }
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