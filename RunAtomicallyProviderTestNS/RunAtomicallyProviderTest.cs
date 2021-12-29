using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using AssertBenchmarkRelatedTestShouldNotRunInDebugProviderNS;
using BenchmarkUtilNS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RunAtomicallyProviderNS;

namespace RunAtomicallyProviderTestNS;

[TestClass]
public class RunAtomicallyProviderTest
{
    private const int AccessCount = (int) 1e5;
    private const int BenchmarkCount = (int) 3e5;

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
        .Select(singleThreadSchedulerFactory => new[] {singleThreadSchedulerFactory});

    private static IEnumerable<object[]> MultipleThreadSchedulerFactoryListTestData =>
        MultipleThreadSchedulerFactoryList
            .Select(multipleThreadSchedulerFactory => new[] {multipleThreadSchedulerFactory});

    [TestMethod]
    [DynamicData(nameof(MultipleThreadSchedulerFactoryListTestData))]
    public void AtomicIncrementUnsafeShouldNotCauseRaceWithMultipleThreadScheduler(
        Func<IScheduler> schedulerFactory
    )
    {
        RunWithSchedulerAndHandleDisposal(
            scheduler =>
            {
                var shardedSpinningAsyncAtomicScheduler = new RunAtomicallyProvider();
                Assert.IsFalse(
                    CausesRaceIncrementFuncWithScheduler(
                        countProvider =>
                        {
                            shardedSpinningAsyncAtomicScheduler.RunAtomically(countProvider.IncrementUnsafe);
                        },
                        scheduler
                    )
                );
            },
            schedulerFactory
        );
    }

    [TestMethod]
    [DynamicData(nameof(SingleThreadSchedulerListTestData))]
    public void AtomicIncrementUnsafeShouldNotCauseRaceWithSingleThreadScheduler(Func<IScheduler> schedulerFactory)
    {
        RunWithSchedulerAndHandleDisposal(
            scheduler =>
            {
                var shardedSpinningAsyncAtomicScheduler = new RunAtomicallyProvider();
                Assert.IsFalse(
                    CausesRaceIncrementFuncWithScheduler(
                        countProvider =>
                        {
                            shardedSpinningAsyncAtomicScheduler.RunAtomically(countProvider.IncrementUnsafe);
                        },
                        scheduler
                    )
                );
            },
            schedulerFactory
        );
    }

    [DataTestMethod]
    [DynamicData(nameof(MultipleThreadSchedulerFactoryListTestData))]
    public void IncrementUnsafeShouldCauseRaceWithMultipleThreadScheduler(Func<IScheduler> schedulerFactory)
    {
        RunWithSchedulerAndHandleDisposal(
            scheduler =>
            {
                Assert.IsTrue(
                    CausesRaceIncrementFuncWithScheduler(
                        unsafeCountProvider => unsafeCountProvider.IncrementUnsafe(),
                        scheduler
                    )
                );
            },
            schedulerFactory
        );
    }

    [DataTestMethod]
    [DynamicData(nameof(SingleThreadSchedulerListTestData))]
    public void IncrementUnsafeShouldNotCauseRaceWithSingleThreadScheduler(Func<IScheduler> schedulerFactory)
    {
        RunWithSchedulerAndHandleDisposal(
            scheduler =>
            {
                Assert.IsFalse(
                    CausesRaceIncrementFuncWithScheduler(
                        unsafeCountProvider => unsafeCountProvider.IncrementUnsafe(),
                        scheduler
                    )
                );
            },
            schedulerFactory
        );
    }

    [TestMethod]
    [DynamicData(nameof(MultipleThreadSchedulerFactoryListTestData))]
    public async Task ShouldSpendLessProcessorTimeThanAlternativesWithMultipleThreadScheduler(
        Func<IScheduler> schedulerFactory
    )
    {
        AssertBenchmarkRelatedTestShouldNotRunInDebugProvider.AssertBenchmarkRelatedTestShouldNotRunInDebug();

        await RunWithSchedulerAndHandleDisposal(
            async scheduler =>
            {
                Action mockAction = () =>
                {
                    // Spinning long enough to guarantee overlap => lock contention
                    Thread.SpinWait(20);
                };

                var benchmarkList = new Func<Task<BenchmarkResult<Unit>[]>>[]
                {
                    () =>
                    {
                        var shardedSpinningAsyncAtomicScheduler =
                            new RunAtomicallyProvider(Environment.ProcessorCount);
                        return Task.WhenAll(Enumerable
                            .Range(1, BenchmarkCount)
                            .Select(i =>
                            {
                                return BenchmarkUtil.BenchmarkOnScheduler(() =>
                                {
                                    shardedSpinningAsyncAtomicScheduler.RunAtomically(mockAction);
                                    return Unit.Default;
                                }, scheduler);
                            }));
                    },
                    () =>
                    {
                        var spinLock = new SpinLock();
                        return Task.WhenAll(Enumerable
                            .Range(1, BenchmarkCount)
                            .Select(i =>
                            {
                                return BenchmarkUtil.BenchmarkOnScheduler(() =>
                                {
                                    var lockTaken = false;
                                    spinLock.Enter(ref lockTaken);
                                    mockAction();
                                    spinLock.Exit();
                                    return Unit.Default;
                                }, scheduler);
                            }));
                    },
                    () =>
                    {
                        var gate = new object();
                        return Task.WhenAll(Enumerable
                            .Range(1, BenchmarkCount)
                            .Select(i =>
                            {
                                return BenchmarkUtil.BenchmarkOnScheduler(() =>
                                {
                                    lock (gate)
                                    {
                                        mockAction();
                                    }

                                    return Unit.Default;
                                }, scheduler);
                            }));
                    }
                };

                var benchmarkDurationList = new List<TimeSpan>();

                foreach (var benchmark in benchmarkList)
                    benchmarkDurationList.Add((await RunWithoutGCProvider.RunWithoutGC(benchmark))
                        .Select(benchmarkResult => benchmarkResult.TimePassed)
                        .Aggregate((a, b) => a + b)
                    );

                foreach (var benchmarkDuration in benchmarkDurationList) Console.WriteLine(benchmarkDuration);
                Assert.IsTrue(benchmarkDurationList.First() == benchmarkDurationList.Min());
            },
            schedulerFactory
        );
    }

    private static bool CausesRaceIncrementFuncWithScheduler(
        Action<CountProvider> incrementFunc,
        IScheduler scheduler
    )
    {
        var countProvider = new CountProvider();
        var countdownEvent = new CountdownEvent(AccessCount);
        for (var i = 0; i < AccessCount; i++)
            scheduler.Schedule(() =>
            {
                incrementFunc(countProvider);
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
        action(scheduler);
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
            await action(scheduler);
        }
        finally
        {
            if (scheduler is IDisposable disposableScheduler) disposableScheduler.Dispose();
        }
    }
}