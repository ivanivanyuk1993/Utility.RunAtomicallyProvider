using ShardedQueueNS;

namespace RunAtomicallyAsyncProviderNS;

/// <summary>
///     In some cases we can not use <see cref="RunAtomicallyProvider"/>,
///     because we need to pause execution until some task completes
///
///     We could do it in <see cref="RunAtomicallyProvider"/>,
///     but then we would need switch by whether function async or not,
///     and we want to avoid it, because <see cref="RunAtomicallyProvider"/> should be light-weight
/// </summary>
public class RunAtomicallyAsyncProvider
{
    protected readonly ShardedQueue<Func<Task>> AsyncActionQueue;

    protected int AsyncActionCount;

    public RunAtomicallyAsyncProvider() : this(maxConcurrentThreadCount: Environment.ProcessorCount)
    {
    }

    public RunAtomicallyAsyncProvider(int maxConcurrentThreadCount)
    {
        AsyncActionQueue = new ShardedQueue<Func<Task>>(maxConcurrentThreadCount: maxConcurrentThreadCount);
    }

    public void RunAtomicallyAsyncAction(Func<Task> asyncAction)
    {
        if (Interlocked.Increment(location: ref AsyncActionCount) != 1)
        {
            AsyncActionQueue.Enqueue(item: asyncAction);
        }
        else
        {
#pragma warning disable CS4014
            StartRunningAsyncActionLoop(asyncAction);
#pragma warning restore CS4014
        }
    }

    protected virtual async Task StartRunningAsyncActionLoop(Func<Task> firstAsyncAction)
    {
        // If we acquired first lock, firstAsyncAction should be executed immediately and firstAsyncAction loop started
        await firstAsyncAction();
        // Note that if Interlocked.Decrement(ref AsyncActionCount) != 0
        // => some Thread entered first if block in method
        // => Enqueue is guaranteed to be called
        // => Dequeue() will not deadlock while spins until it gets item
        while (Interlocked.Decrement(location: ref AsyncActionCount) != 0)
            await AsyncActionQueue.DequeueOrSpin()();
    }
}