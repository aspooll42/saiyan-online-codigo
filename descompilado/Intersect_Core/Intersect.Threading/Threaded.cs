using System;
using System.Threading;

namespace Intersect.Threading;

public abstract class Threaded : IDisposable
{
	private readonly Thread mThread;

	private bool mDisposed;

	protected Threaded(string name = null)
	{
		mThread = new Thread(ThreadStartWrapper);
		if (!string.IsNullOrEmpty(name))
		{
			mThread.Name = name;
		}
	}

	public void Dispose()
	{
		if (!mDisposed)
		{
			mThread.Interrupt();
			mDisposed = true;
		}
	}

	public Thread Start(params object[] args)
	{
		mThread.Start(args);
		return mThread;
	}

	private void ThreadStartWrapper(object args)
	{
		ThreadStart(args as object[]);
	}

	protected abstract void ThreadStart(params object[] args);
}
public abstract class Threaded<TArgument> : Threaded
{
	protected Threaded(string name = null)
		: base(name)
	{
	}

	protected override void ThreadStart(params object[] args)
	{
		if (args == null || args.Length < 1)
		{
			throw new ArgumentOutOfRangeException("args", "Expected one argument of type " + typeof(TArgument).FullName + ".");
		}
		ThreadStart((TArgument)args[0]);
	}

	protected abstract void ThreadStart(TArgument argument);
}
