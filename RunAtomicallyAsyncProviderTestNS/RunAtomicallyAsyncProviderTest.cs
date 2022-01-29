using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using CountProviderNS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RunAtomicallyAsyncProviderNS;

namespace RunAtomicallyAsyncProviderTestNS;

[TestClass]
public class RunAtomicallyAsyncProviderTest
{
    private const int AccessCount = (int) 1e5;

    private static readonly Func<IScheduler>[] SingleThreadSchedulerFactoryList =
    {
        () => new EventLoopScheduler(),
        () => Scheduler.CurrentThread,
        () => Scheduler.Immediate
    };

    private static readonly Func<IScheduler>[] MultipleThreadSchedulerFactoryList =
    {
        () => ThreadPoolScheduler.Instance
    };

    private static IEnumerable<object[]> SingleThreadSchedulerListTestData => SingleThreadSchedulerFactoryList
        .Select(selector: singleThreadSchedulerFactory => new[] {singleThreadSchedulerFactory});

    private static IEnumerable<object[]> MultipleThreadSchedulerFactoryListTestData =>
        MultipleThreadSchedulerFactoryList
            .Select(selector: multipleThreadSchedulerFactory => new[] {multipleThreadSchedulerFactory});

    [TestMethod]
    [DynamicData(dynamicDataSourceName: nameof(MultipleThreadSchedulerFactoryListTestData))]
    public void AtomicIncrementUnsafeShouldNotCauseRaceWithMultipleThreadScheduler(
        Func<IScheduler> schedulerFactory
    )
    {
        RunWithSchedulerAndHandleDisposal(
            action: scheduler =>
            {
                var shardedSpinningAsyncAtomicScheduler = new RunAtomicallyAsyncProvider();
                Assert.IsFalse(
                    condition: CausesRaceIncrementFuncWithScheduler(
                        incrementFunc: countProvider =>
                        {
                            shardedSpinningAsyncAtomicScheduler.RunAtomicallyAsyncAction(
                                asyncAction: countProvider.IncrementUnsafeAsync);
                        },
                        scheduler: scheduler
                    )
                );
            },
            schedulerFactory: schedulerFactory
        );
    }

    [TestMethod]
    [DynamicData(dynamicDataSourceName: nameof(SingleThreadSchedulerListTestData))]
    public void AtomicIncrementUnsafeShouldNotCauseRaceWithSingleThreadScheduler(Func<IScheduler> schedulerFactory)
    {
        RunWithSchedulerAndHandleDisposal(
            action: scheduler =>
            {
                var shardedSpinningAsyncAtomicScheduler = new RunAtomicallyAsyncProvider();
                Assert.IsFalse(
                    condition: CausesRaceIncrementFuncWithScheduler(
                        incrementFunc: countProvider =>
                        {
                            shardedSpinningAsyncAtomicScheduler.RunAtomicallyAsyncAction(
                                asyncAction: countProvider.IncrementUnsafeAsync);
                        },
                        scheduler: scheduler
                    )
                );
            },
            schedulerFactory: schedulerFactory
        );
    }

    [DataTestMethod]
    [DynamicData(dynamicDataSourceName: nameof(MultipleThreadSchedulerFactoryListTestData))]
    public void IncrementUnsafeShouldCauseRaceWithMultipleThreadScheduler(Func<IScheduler> schedulerFactory)
    {
        RunWithSchedulerAndHandleDisposal(
            action: scheduler =>
            {
                Assert.IsTrue(
                    condition: CausesRaceIncrementFuncWithScheduler(
                        incrementFunc: unsafeCountProvider => unsafeCountProvider.IncrementUnsafe(),
                        scheduler: scheduler
                    )
                );
            },
            schedulerFactory: schedulerFactory
        );
    }

    [DataTestMethod]
    [DynamicData(dynamicDataSourceName: nameof(SingleThreadSchedulerListTestData))]
    public void IncrementUnsafeShouldNotCauseRaceWithSingleThreadScheduler(Func<IScheduler> schedulerFactory)
    {
        RunWithSchedulerAndHandleDisposal(
            action: scheduler =>
            {
                Assert.IsFalse(
                    condition: CausesRaceIncrementFuncWithScheduler(
                        incrementFunc: unsafeCountProvider => unsafeCountProvider.IncrementUnsafe(),
                        scheduler: scheduler
                    )
                );
            },
            schedulerFactory: schedulerFactory
        );
    }

    private static bool CausesRaceIncrementFuncWithScheduler(
        Action<CountProvider> incrementFunc,
        IScheduler scheduler
    )
    {
        var countProvider = new CountProvider();
        var countdownEvent = new CountdownEvent(initialCount: AccessCount);
        for (var i = 0; i < AccessCount; i++)
            scheduler.Schedule(action: () =>
            {
                incrementFunc(obj: countProvider);
                countdownEvent.Signal();
            });
        countdownEvent.Wait();

        return AccessCount != countProvider.Count;
    }

    /// <summary>
    ///     todo make generic and DRY
    /// </summary>
    private void RunWithSchedulerAndHandleDisposal(Action<IScheduler> action, Func<IScheduler> schedulerFactory)
    {
        var scheduler = schedulerFactory();
        action(obj: scheduler);
        if (scheduler is IDisposable disposableScheduler) disposableScheduler.Dispose();
    }

    /// <summary>
    ///     todo make generic and DRY
    /// </summary>
    private async Task RunWithSchedulerAndHandleDisposal(
        Func<IScheduler, Task> action,
        Func<IScheduler> schedulerFactory
    )
    {
        var scheduler = schedulerFactory();
        try
        {
            await action(arg: scheduler);
        }
        finally
        {
            if (scheduler is IDisposable disposableScheduler) disposableScheduler.Dispose();
        }
    }
}