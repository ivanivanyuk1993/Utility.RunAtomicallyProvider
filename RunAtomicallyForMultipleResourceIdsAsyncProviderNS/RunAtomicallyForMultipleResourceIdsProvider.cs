// using System.Collections.Concurrent;
// using IsLockedAndQueueNS;
// using RunAsyncActionAtomicallyProviderNS;
// using SpinLockUtilNS;
//
// namespace RunAtomicallyForMultipleResourceIdsAsyncProviderNS;
//
// /// <summary>
// ///     todo make it work with different <see cref="IScheduler" />-s, not only with default scheduler for tasks(ThreadPool)
// /// </summary>
// /// <typeparam name="TResourceId"></typeparam>
// public class RunAtomicallyForMultipleResourceIdsAsyncProvider<TResourceId> where TResourceId : notnull
// {
//     private readonly ConcurrentDictionary<TResourceId, ResourceQueue>
//         _resourceIdToQueueMap = new();
//
//     private readonly RunAsyncActionAtomicallyProvider _runAsyncActionAtomicallyProvider = new();
//
//     /// <summary>
//     /// </summary>
//     /// <param name="action">Notice that order of execution not guaranteed, only atomicity is guaranteed</param>
//     /// <param name="uniqueResourceIdList">
//     ///     Please pass only unique resource ids here,
//     ///     because we want to make this methods as cheap as possible,
//     ///     when/if you need to add uniqueness check, add additional method,
//     ///     which will call this one
//     ///     Uniqueness is needed to get correct length
//     /// </param>
//     /// <returns>
//     ///     We do not need to return <see cref="Task" />, here, because even if we want to lock root node, then leaf
//     ///     nodes, because we usually need to lock one node
//     /// </returns>
//     public Task RunAtomicallyForMultipleResourceIdsAsync(
//         Action action,
//         IList<TResourceId> uniqueResourceIdList
//     )
//     {
//         // When this count will reach 0, it will mean that all id queues reached it and wait for it, and no action is
//         // being executed on id from list, hence we can run action
//         var actionFinishedRunTaskCompletionSource = new TaskCompletionSource();
//         var countOfIdQueuesWhichStillDidntReachThisAction = uniqueResourceIdList.Count;
//
//         // We need to handle all keys atomically, or else we may have situation when keys were added in random order
//         // and queue 1 waits 2, while queue 2 waits 1
//         _runAsyncActionAtomicallyProvider.RunAsyncActionAtomically(asyncAction: async () =>
//         {
//             // We could check if count is 1 here and add simpler action in this case, but we expect it to be a rare case,
//             // hence we will do more complex(than possible for count = 1) action in this case, but avoid unnecessary
//             // for other cases check
//             var addActionToQueueOfResourceIdTaskList = uniqueResourceIdList
//                 .Select(selector: async resourceId =>
//                 {
//                     IsLockedAndQueue<Func<Task>>? addedHereDoOrWaitAsyncQueue = null;
//                     var doOrWaitAsyncQueue = _resourceIdToQueueMap.GetOrAdd(
//                         key: resourceId,
//                         valueFactory: _ =>
//                         {
//                             addedHereDoOrWaitAsyncQueue = new IsLockedAndQueue<Func<Task>>();
//                             addedHereDoOrWaitAsyncQueue.Queue.Enqueue(item: async () =>
//                             {
//                                 if (Interlocked.Decrement(
//                                         location: ref countOfIdQueuesWhichStillDidntReachThisAction) == 0)
//                                 {
//                                     action();
//                                     actionFinishedRunTaskCompletionSource.SetResult();
//                                 }
//                                 else
//                                 {
//                                     await actionFinishedRunTaskCompletionSource.Task;
//                                 }
//
//                                 SpinLockUtil.AcquireLock(isLocked: ref addedHereDoOrWaitAsyncQueue.IsLocked);
//                                 addedHereDoOrWaitAsyncQueue.Queue..Dequeue();
//                                 if (addedHereDoOrWaitAsyncQueue.Queue.TryPeek(result: out var nextAction))
//                                 {
//                                     // run next action
//                                 }
//
//                                 SpinLockUtil.UnlockOne(isLocked: ref addedHereDoOrWaitAsyncQueue.IsLocked);
//                             });
//                             return addedHereDoOrWaitAsyncQueue;
//                         });
//
//                     if (addedHereDoOrWaitAsyncQueue != doOrWaitAsyncQueue)
//                     {
//                         SpinLockUtil.AcquireLock(isLocked: ref doOrWaitAsyncQueue.IsLocked);
//                         doOrWaitAsyncQueue.Queue.Enqueue(item: () =>
//                         {
//                             if (Interlocked.Decrement(location: ref countOfIdQueuesWhichStillDidntReachThisAction) == 0)
//                             {
//                                 action();
//                                 actionFinishedRunTaskCompletionSource.SetResult();
//                             }
//
//                             SpinLockUtil.AcquireLock(isLocked: ref doOrWaitAsyncQueue.IsLocked);
//                             doOrWaitAsyncQueue.Queue..Dequeue();
//                             if (doOrWaitAsyncQueue.Queue.TryPeek(result: out var nextAction))
//                             {
//                                 // run next action
//                             }
//
//                             SpinLockUtil.UnlockOne(isLocked: ref doOrWaitAsyncQueue.IsLocked);
//
//                             return actionFinishedRunTaskCompletionSource.Task;
//                         });
//                         // We can not do it in factory for `ConcurrentDictionary`, because we have no guarantee that
//                         // it will be called once
//                         if (doOrWaitAsyncQueue.Queue.Count == 1) doOrWaitAsyncQueue.Queue.Peek()();
//                         SpinLockUtil.UnlockOne(isLocked: ref doOrWaitAsyncQueue.IsLocked);
//                     }
//
//                     // Notice that, even if all queues wait for longest queue, they don't block `Thread`-s or
//                     // spin or something during this time, in worst case their actions will start after longest
//                     // queue and will be scheduled after longest queue finishes run
//                     //
//                     // We could try to schedule more tasks, if possible, while we wait longest queue, but
//                     // there are following reasons not to do it
//                     // - It is actually logical that actions, which came first, run first
//                     // (notice that we do not guarantee order of operations strongly, order guarantee is weak)
//                     // - It will require complex solution and proof that such solution is scalable on multi-core
//                     // processors and additional logic doesn't require too much synchronization and leads to improvements in
//                     // performance
//                     doOrWaitAsyncQueue.RunAsyncActionAtomically(asyncAction: () =>
//                     {
//                         if (Interlocked.Decrement(location: ref countOfIdQueuesWhichStillDidntReachThisAction) == 0)
//                         {
//                             action();
//                             actionFinishedRunTaskCompletionSource.SetResult();
//                         }
//
//                         return actionFinishedRunTaskCompletionSource.Task;
//                     });
//                 });
//
//             // We want to process items as fast as possible, using all cores, hence we run them simultaneously,
//             // even if it is less efficient than running with less locks
//             await Task.WhenAll(tasks: addActionToQueueOfResourceIdTaskList);
//         });
//
//         return actionFinishedRunTaskCompletionSource.Task;
//     }
// }