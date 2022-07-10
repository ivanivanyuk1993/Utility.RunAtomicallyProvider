using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
    ///     Please don't forget that order of execution is not guaranteed. Atomicity of operations is guaranteed,
    ///     but order can be random
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
    ///     Please don't forget that order of execution is not guaranteed. Atomicity of operations is guaranteed,
    ///     but order can be random
    /// </summary>
    /// <param name="action"></param>
    /// <returns><see cref="IObservable{Unit}" />, which will complete after action was run</returns>
    public IObservable<Unit> RunAtomicallyReactive(Action action)
    {
        return Observable.Create<Unit>(subscribe: observer =>
        {
            RunAtomically(action: () =>
            {
                action();
                // Notice that Rx supports `IScheduler`-s, so these methods can run outside of atomic operation loop,
                // which is efficient
                observer.OnNext(value: Unit.Default);
                observer.OnCompleted();
            });

            // We do not support cancellation, because we expect that cancelling actions is a rare event,
            // and checking if `IsCancellationRequested` every time can take more time than
            // running rarely cancelled action itself
            return Disposable.Empty;
        });
    }

    /// <summary>
    ///     Please don't forget that order of execution is not guaranteed. Atomicity of operations is guaranteed,
    ///     but order can be random
    /// </summary>
    /// <param name="expensiveCancellableAction">
    ///     We want to spend in atomic operation loop as little time as possible(to allow parallel execution),
    ///     hence we allow passing <see cref="CancellationToken" /> to heavy methods
    /// </param>
    /// <returns><see cref="IObservable{Unit}" />, which will complete after action was run</returns>
    public IObservable<Unit> RunAtomicallyReactive(Action<CancellationToken> expensiveCancellableAction)
    {
        return Observable.Defer(observableFactory: () =>
        {
            var cancellationTokenSource = new CancellationTokenSource();

            return Observable
                .Create<Unit>(subscribe: observer =>
                {
                    RunAtomically(action: () =>
                    {
                        if (!cancellationTokenSource.IsCancellationRequested)
                        {
                            expensiveCancellableAction(obj: cancellationTokenSource.Token);
                            // Notice that Rx supports `IScheduler`-s, so these methods can run outside of atomic
                            // operation loop, which is efficient
                            observer.OnNext(value: Unit.Default);
                        }

                        // Notice that Rx supports `IScheduler`-s, so these methods can run outside of atomic
                        // operation loop, which is efficient
                        observer.OnCompleted();
                    });

                    return cancellationTokenSource.Cancel;
                })
                .Finally(finallyAction: () =>
                {
                    // We do want to run disposal outside of atomic operation loop(to support parallelism)
                    cancellationTokenSource.Dispose();
                });
        });
    }

    /// <summary>
    ///     Please don't forget that order of execution is not guaranteed. Atomicity of operations is guaranteed,
    ///     but order can be random
    ///     <returns><see cref="IObservable{Unit}" />, which will complete after action was run</returns>
    /// </summary>
    public IObservable<TResult> RunAtomicallyWithResultObservable<TResult>(Func<TResult> runWithResultFunc)
    {
        return Observable.Create<TResult>(subscribe: observer =>
        {
            RunAtomically(action: () =>
            {
                // Notice that Rx supports `IScheduler`-s, so these methods can run outside of atomic operation loop,
                // which is efficient
                observer.OnNext(value: runWithResultFunc());
                observer.OnCompleted();
            });

            // We do not support cancellation, because we expect that cancelling actions is a rare event,
            // and checking if `IsCancellationRequested` every time can take more time than
            // running rarely cancelled action itself
            return Disposable.Empty;
        });
    }

    /// <summary>
    ///     Please don't forget that order of execution is not guaranteed. Atomicity of operations is guaranteed,
    ///     but order can be random
    /// </summary>
    /// <param name="runWithResultExpensiveFunc">
    ///     We want to spend in atomic operation loop as little time as possible(to allow parallel execution),
    ///     hence we allow passing <see cref="CancellationToken" /> to heavy methods
    /// </param>
    /// <returns><see cref="IObservable{Unit}" />, which will complete after action was run</returns>
    public IObservable<TResult> RunAtomicallyWithResultObservable<TResult>(
        Func<CancellationToken, TResult> runWithResultExpensiveFunc
    )
    {
        return Observable.Defer(observableFactory: () =>
        {
            var cancellationTokenSource = new CancellationTokenSource();

            return Observable
                .Create<TResult>(subscribe: observer =>
                {
                    RunAtomically(action: () =>
                    {
                        if (!cancellationTokenSource.IsCancellationRequested)
                        {
                            // Notice that Rx supports `IScheduler`-s, so these methods can run outside of atomic
                            // operation loop, which is efficient
                            observer.OnNext(value: runWithResultExpensiveFunc(arg: cancellationTokenSource.Token));
                        }

                        // Notice that Rx supports `IScheduler`-s, so these methods can run outside of atomic
                        // operation loop, which is efficient
                        observer.OnCompleted();
                    });

                    return cancellationTokenSource.Cancel;
                })
                .Finally(finallyAction: () =>
                {
                    // We do want to run disposal outside of atomic operation loop(to support parallelism)
                    cancellationTokenSource.Dispose();
                });
        });
    }
}
