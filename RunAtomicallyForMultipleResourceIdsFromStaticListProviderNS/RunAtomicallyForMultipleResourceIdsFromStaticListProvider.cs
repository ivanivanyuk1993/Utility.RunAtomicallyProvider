using System.Collections.Immutable;
using RunAsyncActionAtomicallyProviderNS;
using RunAtomicallyForMultipleResourceIdsProviderNS;

namespace RunAtomicallyForMultipleResourceIdsFromStaticListProviderNS;

/// <summary>
///     <see cref="RunAtomicallyForMultipleResourceIdsFromStaticListProvider{TResourceId}" /> is an experiment to create
///     a perfect fit for cases like work with some limited-length collections, or cases,
///     when we want to run logic with lock on one property,
///     but in other cases - with lock on multiple properties, including first one
///     (like in <see cref="PollSchedulingReactiveValueProvider" />, where we have 3 properties, which we want to
///     lock separately in some cases, and together - in other cases)
///
///     According to benchmarks, in most cases it takes more time than simple
///     <see cref="RunAtomicallyProviderNS.RunAtomicallyProvider"/>, and is much less efficient
///     (requires more synchronization, CPU cycles, which could be spent on serving more requests).
///     It takes less time only under extremely high loads, so todo - revisit it after more processors is the new norm
///
///     Notice that, while <see cref="RunAtomicallyForMultipleResourceIdsFromStaticListProvider{TResourceId}" />,
///     when used with low contention(like for O(1) collection operations, or other short-running operations,
///     or longer running operations on smaller core counts) doesn't save time and adds more synchronization, on longer
///     running async operations it does save time, but in this case you most likely want to use
///     <see cref="IAsyncReadWriteLockForMultipleResourceIds"/>
///
///     todo make it work with different <see cref="IScheduler" />-s, not only with default scheduler for tasks(ThreadPool)
/// </summary>
/// <typeparam name="TResourceId"></typeparam>
public class RunAtomicallyForMultipleResourceIdsFromStaticListProvider<TResourceId> :
    IRunAtomicallyForMultipleResourceIdsProvider<TResourceId> where TResourceId : notnull
{
    private readonly IReadOnlyDictionary<TResourceId, ResourceQueue> _resourceIdToResourceQueueMap;

    private readonly RunAsyncActionAtomicallyProvider _runAsyncActionAtomicallyProvider = new();

    public RunAtomicallyForMultipleResourceIdsFromStaticListProvider(IEnumerable<TResourceId> fullResourceIdList)
    {
        _resourceIdToResourceQueueMap = fullResourceIdList
            .ToImmutableDictionary(
                keySelector: resourceId => resourceId,
                elementSelector: _ => new ResourceQueue()
            );
    }

    /// <summary>
    /// </summary>
    /// <param name="action">Notice that order of execution not guaranteed, only atomicity is guaranteed</param>
    /// <param name="uniqueResourceIdList">
    ///     Please pass only unique resource ids here,
    ///     because we want to make this methods as cheap as possible,
    ///     when/if you need to add uniqueness check, add additional method,
    ///     which will call this one
    ///     Uniqueness is needed to get correct length
    /// </param>
    /// <returns>
    ///     We do not need to return <see cref="Task" />, here, because even if we want to lock root node, then leaf
    ///     nodes, because we usually need to lock one node
    /// </returns>
    public Task RunAtomicallyForMultipleResourceIdsAndGetAwaiter(Action action, IList<TResourceId> uniqueResourceIdList)
    {
        // When this count will reach 0, it will mean that all id queues reached it and wait for it, and no action is
        // being executed on id from list, hence we can run action
        var actionFinishedRunTaskCompletionSource = new TaskCompletionSource();
        var countOfIdQueuesWhichStillDidntReachThisAction = uniqueResourceIdList.Count;

        // We need to handle all keys atomically, or else we may have situation when keys were added in random order
        // and queue 1 waits 2, while queue 2 waits 1
        _runAsyncActionAtomicallyProvider.RunAsyncActionAtomically(asyncAction: async () =>
        {
            // We could check if count is 1 here and add simpler action in this case, but we expect it to be a rare case,
            // hence we will do more complex(than possible for count = 1) action in this case, but avoid unnecessary
            // for other cases check
            var addActionToQueueOfResourceIdTaskList = uniqueResourceIdList
                .Select(selector: async resourceId =>
                {
                    var resourceQueue = _resourceIdToResourceQueueMap[key: resourceId];

                    // Notice that, even if all queues wait for longest queue, they don't block `Thread`-s or
                    // spin or something during this time, in worst case their actions will start after longest
                    // queue and will be scheduled after longest queue finishes run
                    //
                    // We could try to schedule more tasks, if possible, while we wait longest queue, but
                    // there are following reasons not to do it
                    // - It is actually logical that actions, which came first, run first
                    // (notice that we do not guarantee order of operations strongly, order guarantee is weak)
                    // - It will require complex solution and proof that such solution is scalable on multi-core
                    // processors and additional logic doesn't require too much synchronization and leads to improvements in
                    // performance
                    //
                    // It runs in random order, but we do not care, because we always access queue atomically,
                    // and always check length, and wait until resource ids are run in in global
                    // `_runAsyncActionAtomicallyProvider`
                    await resourceQueue.RunAtomicallyProvider.RunAtomicallyAndGetAwaiter(action: () =>
                    {
                        resourceQueue.RunOrWaitAsyncActionQueue.Enqueue(item: async () =>
                        {
                            if (Interlocked.Decrement(location: ref countOfIdQueuesWhichStillDidntReachThisAction) == 0)
                            {
                                action();
                                actionFinishedRunTaskCompletionSource.SetResult();
                            }

                            await actionFinishedRunTaskCompletionSource.Task;

                            resourceQueue.RunAtomicallyProvider.RunAtomically(action: () =>
                            {
                                resourceQueue.RunOrWaitAsyncActionQueue.Dequeue();
                                if (
                                    resourceQueue.RunOrWaitAsyncActionQueue.TryPeek(
                                        result: out var nextRunOrWaitAsyncAction
                                    )
                                )
                                {
                                    nextRunOrWaitAsyncAction();
                                }
                            });
                        });

                        if (resourceQueue.RunOrWaitAsyncActionQueue.Count == 1)
                        {
                            resourceQueue.RunOrWaitAsyncActionQueue.Peek()();
                        }
                    });
                });

            // We want to process items as fast as possible, using all cores, hence we run them simultaneously,
            // even if it is less efficient than running with less locks
            await Task.WhenAll(tasks: addActionToQueueOfResourceIdTaskList);
        });

        return actionFinishedRunTaskCompletionSource.Task;
    }

    /// <summary>
    /// </summary>
    /// <param name="asyncAction">Notice that order of execution not guaranteed, only atomicity is guaranteed</param>
    /// <param name="uniqueResourceIdList">
    ///     Please pass only unique resource ids here,
    ///     because we want to make this methods as cheap as possible,
    ///     when/if you need to add uniqueness check, add additional method,
    ///     which will call this one
    ///     Uniqueness is needed to get correct length
    /// </param>
    /// <returns>
    ///     We do not need to return <see cref="Task" />, here, because even if we want to lock root node, then leaf
    ///     nodes, because we usually need to lock one node
    /// </returns>
    public Task RunAtomicallyForMultipleResourceIdsAndGetAwaiter(Func<Task> asyncAction, IList<TResourceId> uniqueResourceIdList)
    {
        // When this count will reach 0, it will mean that all id queues reached it and wait for it, and no action is
        // being executed on id from list, hence we can run action
        var actionFinishedRunTaskCompletionSource = new TaskCompletionSource();
        var countOfIdQueuesWhichStillDidntReachThisAction = uniqueResourceIdList.Count;

        // We need to handle all keys atomically, or else we may have situation when keys were added in random order
        // and queue 1 waits 2, while queue 2 waits 1
        _runAsyncActionAtomicallyProvider.RunAsyncActionAtomically(asyncAction: async () =>
        {
            // We could check if count is 1 here and add simpler action in this case, but we expect it to be a rare case,
            // hence we will do more complex(than possible for count = 1) action in this case, but avoid unnecessary
            // for other cases check
            var addActionToQueueOfResourceIdTaskList = uniqueResourceIdList
                .Select(selector: async resourceId =>
                {
                    var resourceQueue = _resourceIdToResourceQueueMap[key: resourceId];

                    // Notice that, even if all queues wait for longest queue, they don't block `Thread`-s or
                    // spin or something during this time, in worst case their actions will start after longest
                    // queue and will be scheduled after longest queue finishes run
                    //
                    // We could try to schedule more tasks, if possible, while we wait longest queue, but
                    // there are following reasons not to do it
                    // - It is actually logical that actions, which came first, run first
                    // (notice that we do not guarantee order of operations strongly, order guarantee is weak)
                    // - It will require complex solution and proof that such solution is scalable on multi-core
                    // processors and additional logic doesn't require too much synchronization and leads to improvements in
                    // performance
                    //
                    // It runs in random order, but we do not care, because we always access queue atomically,
                    // and always check length, and wait until resource ids are run in in global
                    // `_runAsyncActionAtomicallyProvider`
                    await resourceQueue.RunAtomicallyProvider.RunAtomicallyAndGetAwaiter(action: () =>
                    {
                        resourceQueue.RunOrWaitAsyncActionQueue.Enqueue(item: async () =>
                        {
                            if (Interlocked.Decrement(location: ref countOfIdQueuesWhichStillDidntReachThisAction) == 0)
                            {
                                await asyncAction();
                                actionFinishedRunTaskCompletionSource.SetResult();
                            }

                            await actionFinishedRunTaskCompletionSource.Task;

                            resourceQueue.RunAtomicallyProvider.RunAtomically(action: () =>
                            {
                                resourceQueue.RunOrWaitAsyncActionQueue.Dequeue();
                                if (
                                    resourceQueue.RunOrWaitAsyncActionQueue.TryPeek(
                                        result: out var nextRunOrWaitAsyncAction
                                    )
                                )
                                {
                                    nextRunOrWaitAsyncAction();
                                }
                            });
                        });

                        if (resourceQueue.RunOrWaitAsyncActionQueue.Count == 1)
                        {
                            resourceQueue.RunOrWaitAsyncActionQueue.Peek()();
                        }
                    });
                });

            // We want to process items as fast as possible, using all cores, hence we run them simultaneously,
            // even if it is less efficient than running with less locks
            await Task.WhenAll(tasks: addActionToQueueOfResourceIdTaskList);
        });

        return actionFinishedRunTaskCompletionSource.Task;
    }
}