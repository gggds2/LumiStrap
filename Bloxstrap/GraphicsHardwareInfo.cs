using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Bloxstrap
{
    public sealed class GraphicsHardwareInfo
    {
        public string Processor { get; init; } = "No disponible";
        public int LogicalProcessors { get; init; }
        public double MemoryGb { get; init; }
        public string GraphicsAdapter { get; init; } = "No disponible";
        public string RecommendedPreset { get; init; } = "balanced";
        public string RecommendationReason { get; init; } = "";

        public string HardwareSummary => $"Procesador: {Processor} ({LogicalProcessors} procesadores lógicos)\nMemoria: {MemoryGb:0.#} GB\nGráficos: {GraphicsAdapter}";
        public string RecommendationName => RecommendedPreset switch
        {
            "performance" => "Rendimiento",
            "quality" => "Calidad",
            _ => "Equilibrio"
        };

        public string PresetPreview => RecommendedPreset switch
        {
            "performance" => "Mallas: mínimo · MSAA: 1x · Texturas: nivel 0 · FRM: 1",
            "quality" => "Mallas: máximo · MSAA: 4x · Texturas: nivel 3 · FRM: 21",
            _ => "Mallas: alto · MSAA: 2x · Texturas: nivel 2 · FRM: 11"
        };

        public static GraphicsHardwareInfo Detect()
        {
            int cores = Environment.ProcessorCount;
            double memory = Math.Round(GetTotalPhysicalMemory() / 1024d / 1024d / 1024d, 1);
            string processor = GetProcessorName();
            string adapter = GetGraphicsAdapter();

            bool basicAdapter = adapter.Contains("Microsoft Basic", StringComparison.OrdinalIgnoreCase)
                || adapter.Contains("Intel(R) HD Graphics", StringComparison.OrdinalIgnoreCase);

            string preset = memory < 8 || cores <= 4 || basicAdapter
                ? "performance"
                : memory >= 16 && cores >= 8 && !adapter.Equals("No disponible", StringComparison.OrdinalIgnoreCase)
                    ? "quality"
                    : "balanced";

            string reason = preset switch
            {
                "performance" => $"Se recomienda priorizar fluidez: el equipo tiene {memory:0.#} GB de memoria y {cores} procesadores lógicos{(basicAdapter ? ", y un adaptador gráfico básico o integrado" : "")}.",
                "quality" => $"El equipo dispone de {memory:0.#} GB de memoria y {cores} procesadores lógicos, así que puede empezar con el perfil de mayor calidad.",
                _ => $"El equipo tiene {memory:0.#} GB de memoria y {cores} procesadores lógicos. Equilibrio es un punto medio prudente para empezar."
            };

            return new GraphicsHardwareInfo
            {
                Processor = processor,
                LogicalProcessors = cores,
                MemoryGb = memory,
                GraphicsAdapter = adapter,
                RecommendedPreset = preset,
                RecommendationReason = reason
            };
        }

        private static long GetTotalPhysicalMemory()
        {
            var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            return GlobalMemoryStatusEx(ref status) ? (long)status.ullTotalPhys : GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        }

        private static string GetProcessorName()
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                return key?.GetValue("ProcessorNameString") as string ?? $"CPU ({Environment.ProcessorCount})";
            }
            catch
            {
                return $"CPU ({Environment.ProcessorCount})";
            }
        }

        private static string GetGraphicsAdapter()
        {
            try
            {
                var device = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };
                for (uint index = 0; EnumDisplayDevices(null, index, ref device, 0); index++)
                {
                    if ((device.StateFlags & 0x1) != 0 && !String.IsNullOrWhiteSpace(device.DeviceString))
                        return device.DeviceString;

                    device = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };
                }
            }
            catch { }

            return "No disponible";
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
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

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
    }
}
