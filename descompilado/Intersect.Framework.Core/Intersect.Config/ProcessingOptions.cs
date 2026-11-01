using System;
using System.Runtime.Serialization;
using Intersect.Framework.Annotations;

namespace Intersect.Config;

[RequiresRestart]
public class ProcessingOptions
{
	public bool CpsLock { get; set; } = true;

	public int MinLogicThreads { get; set; } = 2;

	public int MaxLogicThreads { get; set; } = 4;

	public int LogicThreadIdleTimeout { get; set; } = 20000;

	public int MinNetworkThreads { get; set; } = 2;

	public int MaxNetworkThreads { get; set; } = 4;

	public int NetworkThreadIdleTimeout { get; set; } = 20000;

	public int MinDatabaseThreads { get; set; } = 2;

	public int MaxDatabaseThreads { get; set; } = 4;

	public int DatabaseThreadIdleTimeout { get; set; } = 20000;

	public int DatabaseSaveServerVariablesInterval { get; set; } = 60000;

	public int MinSystemThreadpoolWorkerThreads { get; set; } = -1;

	public int MaxSystemThreadpoolWorkerThreads { get; set; } = -1;

	public int MinSystemThreadpoolIOThreads { get; set; } = -1;

	public int MaxSystemThreadpoolIOThreads { get; set; } = -1;

	public int MapUpdateInterval { get; set; } = 50;

	public int ProjectileUpdateInterval { get; set; } = 50;

	public int PlayerSaveInterval { get; set; } = 120000;

	public int CommonEventAutorunStartInterval { get; set; } = 500;

	public int StaleCooldownRemovalTimer { get; set; } = 60000;

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		Validate();
	}

	public void Validate()
	{
		if (MinLogicThreads < 1)
		{
			throw new InvalidOperationException("Need at least 1 logic thread.");
		}
		if (MaxLogicThreads < MinLogicThreads)
		{
			throw new InvalidOperationException("The maximum number of logic threads should be greater than the minimum number of logic threads.");
		}
		if (LogicThreadIdleTimeout < 1000)
		{
			throw new Exception("Logic thread idle timeout is too low, should be above 1000ms else you may run into significant overhead due to threads being created/destroyed too often.");
		}
		if (MinNetworkThreads < 1)
		{
			throw new InvalidOperationException("Need at least 1 network thread.");
		}
		if (MaxNetworkThreads < MinNetworkThreads)
		{
			throw new InvalidOperationException("The maximum number of network threads should be greater than the minimum number of network threads.");
		}
		if (NetworkThreadIdleTimeout < 1000)
		{
			throw new Exception("Network thread idle timeout is too low, should be above 1000ms else you may run into significant overhead due to threads being created/destroyed too often.");
		}
		if (MinDatabaseThreads < 1)
		{
			throw new InvalidOperationException("Need at least 1 database io thread.");
		}
		if (MaxDatabaseThreads < MinDatabaseThreads)
		{
			throw new InvalidOperationException("The maximum number of database io threads should be greater than the minimum number of database io threads.");
		}
		if (DatabaseThreadIdleTimeout < 1000)
		{
			throw new Exception("Database thread idle timeout is too low, should be above 1000ms else you may run into significant overhead due to threads being created/destroyed too often.");
		}
		if (DatabaseSaveServerVariablesInterval < 5000)
		{
			throw new Exception("Server variables should not be saved more often than once per every 5 seconds, extra db writes will hinder performance.");
		}
		if (MapUpdateInterval > 200)
		{
			throw new InvalidOperationException("Map update interval is too high to provide a smooth gaming experience.");
		}
		if (PlayerSaveInterval < 20000)
		{
			throw new InvalidOperationException("Player save interval is too low and would cause performance issues, consider raising.");
		}
		if (StaleCooldownRemovalTimer < 100)
		{
			throw new InvalidOperationException("StaleCooldownRemovalTimer is too low, value should be at least 100ms to avoid running this check every server tick.");
		}
	}
}
