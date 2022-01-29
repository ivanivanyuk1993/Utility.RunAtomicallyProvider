using System.Collections.Concurrent;
using RunAtomicallyAsyncProviderNS;
using RunAtomicallyAsyncProviderWithFinalActionNS;

namespace RunAtomicallyForMultipleResourceIdsAsyncProviderNS;

public class RunAtomicallyForMultipleResourceIdsAsyncProvider<TResourceId> where TResourceId : notnull
{
    private readonly ConcurrentDictionary<TResourceId, RunAtomicallyAsyncProvider>
        _resourceIdToRunAtomicallyAsyncProviderMap = new();

    private readonly RunAtomicallyAsyncProvider _runAtomicallyAsyncProvider = new();

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
    public Task RunAtomicallyForMultipleResourceIdsAsync(
        Action action,
        IList<TResourceId> uniqueResourceIdList
    )
    {
        // When this count will reach 0, it will mean that all id queues reached it and wait for it, and no action is
        // being executed on id from list, hence we can run action
        var actionFinishedRunTaskCompletionSource = new TaskCompletionSource();
        var countOfIdQueuesWhichStillDidntReachThisAction = uniqueResourceIdList.Count;

        // We need to handle all keys atomically, or else we may have situation when keys were added in random order
        // and queue 1 waits 2, while queue 2 waits 1
        _runAtomicallyAsyncProvider.RunAtomicallyAsyncAction(asyncAction: async () =>
        {
            // We could check if count is 1 here and add simpler action in this case, but we expect it to be a rare case,
            // hence we will do more complex(than possible for count = 1) action in this case, but avoid unnecessary
            // for other cases check
            var addTaskToQueueForResourceTaskList = uniqueResourceIdList
                .Select(selector: resourceId =>
                {
                    return Task.Run(action: () =>
                    {
                        var runAtomicallyAsyncProvider = _resourceIdToRunAtomicallyAsyncProviderMap.GetOrAdd(
                            key: resourceId,
                            // Under high load `finalAction` should run rarely
                            valueFactory: _ => new RunAtomicallyAsyncProviderWithFinalAction(finalAction: @this =>
                            {
                                _runAtomicallyAsyncProvider.RunAtomicallyAsyncAction(asyncAction: () =>
                                {
                                    // Tasks can be added only under `_runAtomicallyAsyncProvider`, so we can't have races here
                                    if (@this.IsEmptyVolatile)
                                        // It should always succeed at least once, because removal happens under
                                        // `_runAtomicallyAsyncProvider` for 1 `resourceId`
                                        _resourceIdToRunAtomicallyAsyncProviderMap.TryRemove(
                                            item: new KeyValuePair<TResourceId, RunAtomicallyAsyncProvider>(
                                                key: resourceId,
                                                value: @this
                                            )
                                        );

                                    return Task.CompletedTask;
                                });
                            })
                        );

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
                        runAtomicallyAsyncProvider.RunAtomicallyAsyncAction(asyncAction: () =>
                        {
                            if (Interlocked.Decrement(location: ref countOfIdQueuesWhichStillDidntReachThisAction) == 0)
                            {
                                action();
                                actionFinishedRunTaskCompletionSource.SetResult();
                            }

                            return actionFinishedRunTaskCompletionSource.Task;
                        });
                    });
                });

            // We want to process items as fast as possible, using all cores, hence we run them simultaneously,
            // even if it is less efficient than running with less locks
            await Task.WhenAll(tasks: addTaskToQueueForResourceTaskList);
        });

        return actionFinishedRunTaskCompletionSource.Task;
    }
}