using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace UsbNotify
{

    static class MonToolConfiguration
    {
        public static readonly int REQUEST_TIMEOUT = 200;
        public static readonly int REQUEST_REPEATS = 4;

    }

    public class CapabilitiesLogic : ICapabilitiesLogic
    {


        enum VcpState
        {
            DEFAULT,
            INPUT_SOURCE,
            COLOR_PRESET,
            SKIP
        }

        enum ParseState
        {
            DEFAULT,
            VCP,
            PROT,
            TYPE,
            CMDS,
            MODEL,
            SKIP,
            MSWHQL,
            ASSET_EEP,
            MCCS_VER,
            ERROR
        }


        public Monitor GetMonitorCapabilities(Monitor monitor)
        {
            char[] pszASCIICapabilitiesString;
            uint dwCapabilitiesStringLengthInCharacters = 0;
            bool result = GetCapabilitiesStringLength(monitor.PhysicalMonitor.hPhysicalMonitor, ref dwCapabilitiesStringLengthInCharacters);

            //Slow Monitor fix
            for (int i = 0; i < MonToolConfiguration.REQUEST_REPEATS && result == false; i++)
            {
                System.Threading.Thread.Sleep(MonToolConfiguration.REQUEST_TIMEOUT);
                result = GetCapabilitiesStringLength(monitor.PhysicalMonitor.hPhysicalMonitor, ref dwCapabilitiesStringLengthInCharacters);
            }

            pszASCIICapabilitiesString = new char[dwCapabilitiesStringLengthInCharacters];

            result = CapabilitiesRequestAndCapabilitiesReply(monitor.PhysicalMonitor.hPhysicalMonitor, pszASCIICapabilitiesString, dwCapabilitiesStringLengthInCharacters);

            //Slow Monitor fix

            for (int i = 0; i < MonToolConfiguration.REQUEST_REPEATS && result == false; i++)
            {
                System.Threading.Thread.Sleep(MonToolConfiguration.REQUEST_TIMEOUT);
                result = CapabilitiesRequestAndCapabilitiesReply(monitor.PhysicalMonitor.hPhysicalMonitor, pszASCIICapabilitiesString, dwCapabilitiesStringLengthInCharacters);
            }


            return ParseVcp(new string(pszASCIICapabilitiesString), monitor);


        }

        private Monitor ParseVcp(string capabilityString, Monitor monitor)
        {

            capabilityString = PrepareCapabilityString(capabilityString);
            var capabilities = new List<uint>();
            var inputSources = new List<uint>();
            var colorPresets = new List<uint>();


            ParseState parseState = ParseState.DEFAULT;
            VcpState vcpState = VcpState.DEFAULT;
            uint? vcpCode = null;
            string model = "";
            foreach (string str in capabilityString.Split(' '))
            {


                ParseState beforeCheckParseState = parseState;

                switch (str)
                {
                    case "vcp":
                        parseState = ParseState.VCP;
                        break;
                    case "prot":
                        parseState = ParseState.PROT;
                        break;
                    case "type":
                        parseState = ParseState.TYPE;
                        break;
                    case "model":
                        parseState = ParseState.MODEL;
                        break;
                    case "cmds":
                        parseState = ParseState.CMDS;
                        break;
                    case "mswhql":
                        parseState = ParseState.MSWHQL;
                        break;
                    case "asset_eep":
                        parseState = ParseState.ASSET_EEP;
                        break;
                    case "mccs_ver":
                        parseState = ParseState.MCCS_VER;
                        break;
                }


                if (parseState == beforeCheckParseState)
                    switch (parseState)
                    {

                        case ParseState.VCP:


                            if (str == "(" && vcpCode != null)
                            {
                                switch (vcpCode.Value)
                                {
                                    case (int)VCPFeature.INPUT_SOURCE:
                                        vcpState = VcpState.INPUT_SOURCE;
                                        break;
                                    case (int)VCPFeature.COLOR_PRESET:
                                        vcpState = VcpState.COLOR_PRESET;
                                        break;
                                    default:
                                        vcpState = VcpState.SKIP;
                                        break;
                                }
                            }
                            else if (str == ")")
                            {
                                vcpState = VcpState.DEFAULT;
                            }
                            else if (str != ")" && str != "(" && str != "")
                            {
                                try
                                {
                                    vcpCode = (uint)HexToDecConverter.Convert(str);

                                    switch (vcpState)
                                    {

                                        case VcpState.DEFAULT:
                                            capabilities.Add(vcpCode.Value);
                                            break;
                                        case VcpState.INPUT_SOURCE:
                                            inputSources.Add(vcpCode.Value);
                                            break;
                                        case VcpState.COLOR_PRESET:
                                            colorPresets.Add(vcpCode.Value);
                                            break;
                                        case VcpState.SKIP:
                                            break;
                                    }


                                }
                                catch (FormatException ex)
                                {
                                    parseState = ParseState.ERROR;
                                }
                            }

                            break;

                        case ParseState.MODEL:
                            if (str != "(" && str != ")")
                            {
                                model += str;
                            }

                            break;

                        default:
                            break;
                    }

            }

            monitor.Capabilitys = capabilities;
            monitor.InputSources = inputSources;
            monitor.Model = model;
            monitor.ColorPresets = colorPresets;

            return monitor;
        }

        private string PrepareCapabilityString(string capabilityString)
        {

            capabilityString = capabilityString.Replace(" (", "(");
            capabilityString = capabilityString.Replace("( ", "(");
            capabilityString = capabilityString.Replace("(", " ( ");
            capabilityString = capabilityString.Replace(" )", ")");
            capabilityString = capabilityString.Replace(")", " ) ");

            return capabilityString;

        }








        [DllImport("dxva2.dll", EntryPoint = "GetCapabilitiesStringLength")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCapabilitiesStringLength(IntPtr hMonitor, ref uint pdwCapabilitiesStringLengthInCharacters);

        [DllImport("dxva2.dll", EntryPoint = "CapabilitiesRequestAndCapabilitiesReply")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CapabilitiesRequestAndCapabilitiesReply(IntPtr hMonitor, [Out] char[] pszASCIICapabilitiesString, uint dwCapabilitiesStringLengthInCharacters);

        public bool IsCapable(Monitor monitor, VCPFeature feature)
        {
            return monitor.Capabilitys.Contains((uint)feature);
        }
    }

    public interface ICapabilitiesLogic
    {

        Monitor GetMonitorCapabilities(Monitor monitor);

        bool IsCapable(Monitor monitor, VCPFeature feature);

    }

    public class Monitor
    {

        public PHYSICAL_MONITOR PhysicalMonitor { get; set; }
        public string Model { get; set; }
        public List<uint> Capabilitys { get; set; }
        public List<uint> InputSources { get; set; }
        public List<uint> ColorPresets { get; set; }
    }


    [Flags]
    public enum VCPFeature
    {
        //Preset Operations VCP Codes  Value != 0
        RESTORE_FACTORY_DEFAULTS = 0x04,
        RESTORE_FACTORY_LUMINANCE_DEFAULTS = 0x05,
        RESTORE_FACTORY_COLOR_DEFAULTS = 0x08,
        STORE_RESTORE_SETTINGS = 0xB0,      //01 Store Restore 02

        //Image Adjustment
        LUMINANCE = 0x10,
        CONTRAST = 0x12,
        COLOR_PRESET = 0x14,
        VIDEO_GAIN_RED = 0x16,
        VIDEO_GAIN_GREEN = 0x18,
        VIDEO_GAIN_BLUE = 0x1A,
        VIDEO_BLACK_LEVEL_RED = 0x6C,
        VIDEO_BLACK_LEVEL_GREEN = 0x6E,
        VIDEO_BLACK_LEVEL_BLUE = 0x70,

        // Miscellaneous Functions 
        INPUT_SOURCE = 0x60,

        // Audio Function
        SPEAKER_VOLUME = 0x62
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct PHYSICAL_MONITOR
    {
        public IntPtr hPhysicalMonitor;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szPhysicalMonitorDescription;
    }

    public static class HexToDecConverter
    {

        public static int Convert(string hex)
        {
            return int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
        }

        public static string ConvertBack(int value)
        {
            return value.ToString("X");
        }

    }
}