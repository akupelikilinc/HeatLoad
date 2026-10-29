using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;

namespace HeatLoad;

/// <summary>CPU/RAM from Win32; GPU from NVML when an NVIDIA card exists, else from LibreHardwareMonitor
/// (which also covers integrated GPUs); temperatures from LibreHardwareMonitor via PawnIO (admin).</summary>
internal sealed class WindowsSensors : ISensorSource
{
    private readonly Computer? _computer;
    private readonly IHardware? _gpu;
    private readonly UpdateVisitor _visitor = new();
    private IntPtr _nvmlDevice;
    private bool _nvmlReady;
    private bool _primed;
    private ulong _prevIdle, _prevKernel, _prevUser;

    public WindowsSensors()
    {
        GetSystemTimes(out var idle, out var kernel, out var user);
        _prevIdle = ToUInt64(idle);
        _prevKernel = ToUInt64(kernel);
        _prevUser = ToUInt64(user);

        try
        {
            if (nvmlInit_v2() == 0)
            {
                _nvmlReady = true;
                if (nvmlDeviceGetCount_v2(out var count) == 0 && count > 0 &&
                    nvmlDeviceGetHandleByIndex_v2(0, out var device) == 0)
                    _nvmlDevice = device;
            }
        }
        catch
        {
            _nvmlReady = false;
        }

        try
        {
            // GPU sensors cost about a second of startup, so only open them when NVML cannot answer.
            var useNvml = _nvmlDevice != IntPtr.Zero;
            var computer = new Computer { IsCpuEnabled = true, IsMemoryEnabled = true, IsGpuEnabled = !useNvml };
            computer.Open();
            _computer = computer;
            _gpu = useNvml ? null : PickGpu(computer);
        }
        catch
        {
            _computer = null;
        }
    }

    public Reading Sample()
    {
        double? cpuUsage = null;
        if (GetSystemTimes(out var idle, out var kernel, out var user))
        {
            ulong i = ToUInt64(idle), k = ToUInt64(kernel), u = ToUInt64(user);
            ulong di = i - _prevIdle, dk = k - _prevKernel, du = u - _prevUser;
            _prevIdle = i;
            _prevKernel = k;
            _prevUser = u;
            ulong total = dk + du; // kernel time already includes idle time
            if (_primed && total > 0)
                cpuUsage = Math.Clamp(100.0 * (double)(total - di) / total, 0, 100);
            _primed = true;
        }

        var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        double? ramUsage = null;
        double? ramUsed = null;
        if (GlobalMemoryStatusEx(ref mem))
        {
            ramUsage = mem.dwMemoryLoad;
            ramUsed = (mem.ullTotalPhys - mem.ullAvailPhys) / 1073741824.0;
        }

        double? gpuUsage = null, gpuTemp = null;
        if (_nvmlDevice != IntPtr.Zero)
        {
            if (nvmlDeviceGetUtilizationRates(_nvmlDevice, out var rates) == 0)
                gpuUsage = rates.gpu;
            if (nvmlDeviceGetTemperature(_nvmlDevice, 0, out var temp) == 0)
                gpuTemp = temp;
        }

        double? cpuTemp = null, ramTemp = null;
        if (_computer is not null)
        {
            try
            {
                _computer.Accept(_visitor);

                // No NVIDIA driver to ask: read the card LibreHardwareMonitor found, discrete or integrated.
                if (_gpu is { } gpu)
                {
                    gpuUsage ??= Sensor(gpu, SensorType.Load, "GPU Core", "D3D 3D");
                    gpuTemp ??= Sensor(gpu, SensorType.Temperature, "GPU Core", "GPU Hot Spot");
                }

                double? tdie = null;
                foreach (var hw in _computer.Hardware)
                {
                    if (hw.HardwareType == HardwareType.Cpu)
                    {
                        foreach (var s in hw.Sensors)
                        {
                            if (s.SensorType != SensorType.Temperature || s.Value is not > 0) continue;
                            if (s.Name.Contains("Tctl/Tdie", StringComparison.OrdinalIgnoreCase))
                                cpuTemp = s.Value;
                            else if (s.Name.Contains("Tdie", StringComparison.OrdinalIgnoreCase) && s.Value > tdie)
                                tdie = s.Value;
                        }
                    }
                    else if (hw.HardwareType == HardwareType.Memory)
                    {
                        foreach (var s in hw.Sensors)
                            if (s.SensorType == SensorType.Temperature && s.Value is > 0 &&
                                s.Name.StartsWith("DIMM", StringComparison.OrdinalIgnoreCase))
                                ramTemp = Math.Max(ramTemp ?? 0, s.Value.Value);
                    }
                }
                cpuTemp ??= tdie;
            }
            catch
            {
                // sensor read failure -> keep nulls, usage stays live
            }
        }

        return new Reading(cpuUsage, gpuUsage, ramUsage, cpuTemp, gpuTemp, ramTemp, ramUsed);
    }

    public void Dispose()
    {
        _computer?.Close();
        if (_nvmlReady) nvmlShutdown();
    }

    /// <summary>One GPU for usage and temperature alike: the discrete card when there is one, else the integrated one.</summary>
    // shortcut: an AMD APU next to an AMD card resolves to the first node; pick by GPU memory if that ever matters.
    private static IHardware? PickGpu(Computer computer) =>
        computer.Hardware
            .Where(h => h.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel)
            .OrderBy(h => h.HardwareType switch { HardwareType.GpuNvidia => 0, HardwareType.GpuAmd => 1, _ => 2 })
            .FirstOrDefault();

    /// <summary>First sensor of that type with one of the names; an Intel iGPU reports its 3D node instead of "GPU Core".</summary>
    private static double? Sensor(IHardware hardware, SensorType type, params string[] names)
    {
        foreach (var name in names)
        {
            if (hardware.Sensors.FirstOrDefault(s => s.SensorType == type && s.Name == name) is { Value: { } value })
                return value;
        }
        return null;
    }

    private static ulong ToUInt64(FILETIME ft) => ((ulong)ft.dwHighDateTime << 32) | ft.dwLowDateTime;

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NvmlUtilization
    {
        public uint gpu;
        public uint memory;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("nvml.dll")]
    private static extern int nvmlInit_v2();

    [DllImport("nvml.dll")]
    private static extern int nvmlShutdown();

    [DllImport("nvml.dll")]
    private static extern int nvmlDeviceGetCount_v2(out uint deviceCount);

    [DllImport("nvml.dll")]
    private static extern int nvmlDeviceGetHandleByIndex_v2(uint index, out IntPtr device);

    [DllImport("nvml.dll")]
    private static extern int nvmlDeviceGetUtilizationRates(IntPtr device, out NvmlUtilization utilization);

    [DllImport("nvml.dll")]
    private static extern int nvmlDeviceGetTemperature(IntPtr device, uint sensorType, out uint temp);
}

internal sealed class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer) => computer.Traverse(this);

    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (var sub in hardware.SubHardware) sub.Accept(this);
    }

    public void VisitSensor(ISensor sensor) { }

    public void VisitParameter(IParameter parameter) { }
}
