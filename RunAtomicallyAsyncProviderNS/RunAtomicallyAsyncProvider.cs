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
    private readonly ShardedQueue<Func<Task>> _asyncActionQueue;

    private int _asyncActionCount;

    public RunAtomicallyAsyncProvider() : this(maxConcurrentThreadCount: Environment.ProcessorCount)
    {
    }

    public RunAtomicallyAsyncProvider(int maxConcurrentThreadCount)
    {
        _asyncActionQueue = new ShardedQueue<Func<Task>>(maxConcurrentThreadCount: maxConcurrentThreadCount);
    }

    public Task<T> RunAtomicallyWithResultAsync<T>(Func<Task<T>> asyncFunctionWithResult)
    {
        var taskCompletionSource = new TaskCompletionSource<T>();
        // We want to return `Task` as soon as we added our function to queue, hence we do not `await` here
#pragma warning disable CS4014
        RunAtomicallyAsync(asyncAction: async () =>
#pragma warning restore CS4014
        {
            taskCompletionSource.SetResult(result: await asyncFunctionWithResult());
        });
        return taskCompletionSource.Task;
    }

    public async Task RunAtomicallyAsync(Func<Task> asyncAction)
    {
        if (Interlocked.Increment(location: ref _asyncActionCount) != 1)
        {
            _asyncActionQueue.Enqueue(item: asyncAction);
        }
        else
        {
            // If we acquired first lock, asyncAction should be executed immediately and asyncAction loop started
            await asyncAction();
            // Note that if Interlocked.Decrement(ref _asyncActionCount) != 0
            // => some Thread entered first if block in method
            // => Enqueue is guaranteed to be called
            // => Dequeue() will not deadlock while spins until it gets item
            while (Interlocked.Decrement(location: ref _asyncActionCount) != 0)
                await _asyncActionQueue.DequeueOrSpin()();
        }
    }
}