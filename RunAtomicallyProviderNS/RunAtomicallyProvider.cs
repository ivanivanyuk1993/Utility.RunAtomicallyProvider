using ShardedQueueNS;

namespace RunAtomicallyProviderNS;

/// <summary>
///     <para>
///         <see cref="RunAtomicallyProvider" /> is needed to run actions atomically without thread blocking, or context
///         switch, or spin lock contention, or rescheduling on first thread, which enters <see cref="RunAtomically" />
///     </para>
///     <para>
///         Notice that it uses <see cref="ShardedQueue{T}" />, which doesn't guarantee order of retrieval, hence
///         <see cref="RunAtomicallyProvider" /> doesn't guarantee order of execution too, even of already added
///         items
///     </para>
/// </summary>
public class RunAtomicallyProvider
{
    private readonly ShardedQueue<Action> _actionQueue;

    private int _actionCount;

    public RunAtomicallyProvider() : this(maxConcurrentThreadCount: Environment.ProcessorCount)
    {
    }

    public RunAtomicallyProvider(int maxConcurrentThreadCount)
    {
        _actionQueue = new ShardedQueue<Action>(maxConcurrentThreadCount: maxConcurrentThreadCount);
    }

    /// <summary>
    ///     Please don't forget that order of execution is not guaranteed. Atomicity of operations is guaranteed, but order can
    ///     be random
    /// </summary>
    /// <param name="runAndGetResultFunc"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public Task<T> RunAtomicallyAndGetResultAsync<T>(Func<T> runAndGetResultFunc)
    {
        var taskCompletionSource = new TaskCompletionSource<T>();
        RunAtomically(action: () => { taskCompletionSource.SetResult(result: runAndGetResultFunc()); });
        return taskCompletionSource.Task;
    }

    /// <summary>
    ///     Please don't forget that order of execution is not guaranteed. Atomicity of operations is guaranteed, but order can
    ///     be random
    /// </summary>
    /// <param name="action"></param>
    public void RunAtomically(Action action)
    {
        if (Interlocked.Increment(location: ref _actionCount) != 1)
        {
            _actionQueue.Enqueue(item: action);
        }
        else
        {
            // If we acquired first lock, action should be executed immediately and action loop started
            action();
            // Note that if Interlocked.Decrement(ref _actionCount) != 0
            // => some Thread entered first if block in method
            // => Enqueue is guaranteed to be called
            // => Dequeue() will not deadlock while spins until it gets item
            while (Interlocked.Decrement(location: ref _actionCount) != 0) _actionQueue.DequeueOrSpin()();
        }
    }

    /// <summary>
    ///     Please don't forget that order of execution is not guaranteed. Atomicity of operations is guaranteed, but order can
    ///     be random
    /// </summary>
    /// <param name="action"></param>
    /// <returns><see cref="Task"/>, which will complete after action was run</returns>
    public Task RunAtomicallyAndGetAwaiter(Action action)
    {
        var taskCompletionSource = new TaskCompletionSource();
        RunAtomically(action: () =>
        {
            action();
            taskCompletionSource.SetResult();
        });
        return taskCompletionSource.Task;
    }
}