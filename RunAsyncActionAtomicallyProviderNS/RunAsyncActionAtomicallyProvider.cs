using ShardedQueueNS;

namespace RunAsyncActionAtomicallyProviderNS;

/// <summary>
///     In some cases we can not use <see cref="RunAtomicallyProvider" />,
///     because we need to pause execution until some task completes
///     We could do it in <see cref="RunAtomicallyProvider" />,
///     but then we would need switch by whether function async or not,
///     and we want to avoid it, because <see cref="RunAtomicallyProvider" /> should be light-weight
/// </summary>
public class RunAsyncActionAtomicallyProvider
{
    protected readonly ShardedQueue<Func<Task>> AsyncActionQueue;

    protected int AsyncActionCount;

    public RunAsyncActionAtomicallyProvider() : this(maxConcurrentThreadCount: Environment.ProcessorCount)
    {
    }

    public RunAsyncActionAtomicallyProvider(int maxConcurrentThreadCount)
    {
        AsyncActionQueue = new ShardedQueue<Func<Task>>(maxConcurrentThreadCount: maxConcurrentThreadCount);
    }

    /// <summary>
    ///     Please don't forget that order of execution is not guaranteed. Atomicity of operations is guaranteed, but order can
    ///     be random
    /// </summary>
    /// <param name="asyncAction"></param>
    public void RunAsyncActionAtomically(Func<Task> asyncAction)
    {
        if (Interlocked.Increment(location: ref AsyncActionCount) != 1)
        {
            AsyncActionQueue.Enqueue(item: asyncAction);
        }
        else
        {
            // `StartAsyncActionLoop` is async only to ease logic, there is nothing meaningful to await
#pragma warning disable CS4014
            StartAsyncActionLoop(firstAsyncAction: asyncAction);
#pragma warning restore CS4014
        }
    }

    private async Task StartAsyncActionLoop(Func<Task> firstAsyncAction)
    {
        // If we acquired first lock, `firstAsyncAction` should be executed immediately and firstAsyncAction loop started
        await firstAsyncAction();
        // Note that if Interlocked.Decrement(ref AsyncActionCount) != 0
        // => some Thread entered first if block in method
        // => Enqueue is guaranteed to be called
        // => Dequeue() will not deadlock while spins until it gets item
        while (Interlocked.Decrement(location: ref AsyncActionCount) != 0)
            await AsyncActionQueue.DequeueOrSpin()();
    }
}