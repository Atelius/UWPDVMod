using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace UWPDVMod.Core;

public record HardwareInfo(
    int ScreenWidth, int ScreenHeight, int RefreshRate, int MaxRefreshRate,
    string GpuName, long VramBytes, long RamBytes)
{
    public double VramGb => VramBytes / 1073741824.0;
    public double RamGb => RamBytes / 1073741824.0;
    public int PanelHz => Math.Max(RefreshRate, MaxRefreshRate);
    public float Aspect => ScreenHeight > 0 ? (float)ScreenWidth / ScreenHeight : 16f / 9f;
}

public record Recommendation(string Section, string Key, string NewValue, string Reason);

/// <summary>
/// Hardware detection shared by every profile. Recommendation logic itself lives on
/// each <see cref="IModProfile"/> because it is game-specific.
/// </summary>
public static class SystemAnalyzer
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettingsW(string? deviceName, int modeNum, ref DEVMODE devMode);
    private const int ENUM_CURRENT_SETTINGS = -1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
        public ushort dmSpecVersion, dmDriverVersion, dmSize, dmDriverExtra;
        public uint dmFields;
        public int dmPositionX, dmPositionY;
        public uint dmDisplayOrientation, dmDisplayFixedOutput;
        public short dmColor, dmDuplex, dmYResolution, dmTTOption, dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
        public ushort dmLogPixels;
        public uint dmBitsPerPel, dmPelsWidth, dmPelsHeight, dmDisplayFlags, dmDisplayFrequency;
        public uint dmICMMethod, dmICMIntent, dmMediaType, dmDitherType, dmReserved1, dmReserved2, dmPanningWidth, dmPanningHeight;
    }

    public static HardwareInfo Detect()
    {
        // primary display's current mode (resolution + refresh)
        var dm = new DEVMODE { dmSize = (ushort)Marshal.SizeOf<DEVMODE>() };
        int w = 0, h = 0, hz = 0;
        if (EnumDisplaySettingsW(null, ENUM_CURRENT_SETTINGS, ref dm))
        {
            w = (int)dm.dmPelsWidth;
            h = (int)dm.dmPelsHeight;
            hz = (int)dm.dmDisplayFrequency;
        }

        // the monitor may support more than the desktop currently uses (e.g. 240 Hz panel
        // running a 60 Hz desktop) - enumerate all modes at this resolution for the max
        int maxHz = hz;
        var probe = new DEVMODE { dmSize = (ushort)Marshal.SizeOf<DEVMODE>() };
        for (int i = 0; EnumDisplaySettingsW(null, i, ref probe); i++)
        {
            if (probe.dmPelsWidth == (uint)w && probe.dmPelsHeight == (uint)h)
                maxHz = Math.Max(maxHz, (int)probe.dmDisplayFrequency);
        }

        // GPU name via WMI; VRAM via registry (WMI AdapterRAM is a 32-bit value)
        string gpu = "Unknown GPU";
        long vram = 0;
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, AdapterRAM FROM Win32_VideoController WHERE VideoProcessor IS NOT NULL");
            foreach (var mo in searcher.Get())
            {
                gpu = mo["Name"]?.ToString() ?? gpu;
                vram = Convert.ToInt64(mo["AdapterRAM"] ?? 0L);
                break;
            }
        }
        catch { }
        long regVram = ReadVramFromRegistry();
        if (regVram > vram) vram = regVram;

        long ram = 0;
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var mo in searcher.Get())
                ram = Convert.ToInt64(mo["TotalPhysicalMemory"] ?? 0L);
        }
        catch { }

        return new HardwareInfo(w, h, hz, maxHz, gpu, vram, ram);
    }

    private static long ReadVramFromRegistry()
    {
        // HardwareInformation.qwMemorySize under the display class key is the reliable 64-bit VRAM value
        long best = 0;
        try
        {
            using var cls = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
            if (cls == null) return 0;
            foreach (string sub in cls.GetSubKeyNames())
            {
                if (!sub.StartsWith("0")) continue;
                using var k = cls.OpenSubKey(sub);
                if (k?.GetValue("HardwareInformation.qwMemorySize") is long qw && qw > best)
                    best = qw;
            }
        }
        catch { }
        return best;
    }
}
