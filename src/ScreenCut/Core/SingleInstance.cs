namespace ScreenCut.Core;

/// <summary>Ensures only one ScreenCut runs per user session; later launches signal the first one.</summary>
internal sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\ScreenCut.SingleInstance.6f1c2a0e";
    private const string EventName = @"Local\ScreenCut.Activate.6f1c2a0e";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activateEvent;
    private readonly RegisteredWaitHandle _registration;

    private SingleInstance(Mutex mutex)
    {
        _mutex = mutex;
        _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
        _registration = ThreadPool.RegisterWaitForSingleObject(
            _activateEvent, (_, _) => ActivationRequested?.Invoke(this, EventArgs.Empty), null, Timeout.Infinite, executeOnlyOnce: false);
    }

    /// <summary>Raised on a thread-pool thread when another instance was started.</summary>
    public event EventHandler? ActivationRequested;

    public static SingleInstance? TryAcquire()
    {
        var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (createdNew)
        {
            return new SingleInstance(mutex);
        }

        mutex.Dispose();
        return null;
    }

    public static void SignalExisting()
    {
        if (EventWaitHandle.TryOpenExisting(EventName, out var handle))
        {
            using (handle)
            {
                handle.Set();
            }
        }
    }

    public void Dispose()
    {
        _registration.Unregister(null);
        _activateEvent.Dispose();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
