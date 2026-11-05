using Hardware.Info;
using Microsoft.Extensions.Logging;

namespace Intersect.Framework.SystemInformation;

public class PlatformStatistics
{
	private static readonly HardwareInfo? HardwareInfo;

	public static IGPUStatisticsProvider? GPUStatisticsProvider { get; set; }

	public static ILogger? Logger { get; set; }

	public static long AvailablePhysicalMemory => (long)(HardwareInfo?.MemoryStatus.AvailablePhysical ?? MemoriaGC);

	public static long TotalPhysicalMemory => (long)(HardwareInfo?.MemoryStatus.TotalPhysical ?? MemoriaGC);

	public static long AvailableGPUMemory => GPUStatisticsProvider?.AvailableMemory ?? AvailableSystemMemory;

	public static long TotalGPUMemory => GPUStatisticsProvider?.TotalMemory ?? TotalSystemMemory;

	public static long AvailableSystemMemory => (long)(HardwareInfo?.MemoryStatus.AvailableVirtual ?? MemoriaGC);

	public static long TotalSystemMemory => (long)(HardwareInfo?.MemoryStatus.TotalVirtual ?? MemoriaGC);

	private static ulong MemoriaGC => (ulong)System.GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;

	public static void Refresh()
	{
		try
		{
			HardwareInfo?.RefreshMemoryStatus();
		}
		catch
		{
		}
	}

	static PlatformStatistics()
	{
		try
		{
			try
		{
			try
		{
			try
		{
			try
		{
			try
		{
			try
		{
			try
		{
			try
		{
			try
		{
			HardwareInfo = new HardwareInfo();
		}
		catch
		{
			HardwareInfo = null;
		}
		}
		catch
		{
			HardwareInfo = null;
		}
		}
		catch
		{
			HardwareInfo = null;
		}
		}
		catch
		{
			HardwareInfo = null;
		}
		}
		catch
		{
			HardwareInfo = null;
		}
		}
		catch
		{
			HardwareInfo = null;
		}
		}
		catch
		{
			HardwareInfo = null;
		}
		}
		catch
		{
			HardwareInfo = null;
		}
		}
		catch
		{
			HardwareInfo = null;
		}
		}
		catch
		{
			HardwareInfo = null;
		}
		Refresh();
	}
}
