using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AssertTimeUtilNS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RunAfterProviderNS;
using RunAtomicallyForMultipleResourceIdsFromStaticListProviderNS;
using RunAtomicallyForMultipleResourceIdsProviderNS;

namespace RunAtomicallyForMultipleResourceIdsFromStaticListProviderTestNS;

[TestClass]
public class RunAtomicallyForMultipleResourceIdsFromStaticListProviderTest
{
     static readonly Func<IScheduler>[] MultipleThreadSchedulerFactoryList =
    {
        () => ThreadPoolScheduler.Instance
    };

    private static IEnumerable<object[]> MultipleThreadSchedulerFactoryListTestData =>
        MultipleThreadSchedulerFactoryList
            .Select(selector: multipleThreadSchedulerFactory => new[] {multipleThreadSchedulerFactory});

    [TestMethod]
    public async Task OperationsWithIntersectingResourceIdsShouldNotOverlap()
    {
        var resourceIdList = new[]
        {
            1,
            2
        };
        var runAtomicallyForMultipleResourceIdsFromStaticListProvider =
            new RunAtomicallyForMultipleResourceIdsFromStaticListProvider<int>(fullResourceIdList: resourceIdList);

        var logRecordQueue = new ConcurrentQueue<LogRecord<int>>();

        var resourceIdList1 = resourceIdList
            .Take(count: 1)
            .ToArray();
        var resourceIdList2 = resourceIdList
            .Skip(count: 1)
            .Take(count: 1)
            .ToArray();
        var resourceIdList3 = resourceIdList;

        var safeToUseTimeUnit = await AssertTimeUtil.SafeToUseInTaskAssertsAllowedTimeErrorTask;
        var startTime = DateTimeOffset.UtcNow;

        var runTask1TimeSpan = safeToUseTimeUnit;
        var runTask1 = SimulateRunWithResourceIdListAndLog(
            logRecordQueue: logRecordQueue,
            resourceIdList: resourceIdList1,
            runAtomicallyForMultipleResourceIdsFromStaticListProvider:
            runAtomicallyForMultipleResourceIdsFromStaticListProvider,
            simulatedRunTimeSpan: runTask1TimeSpan
        );
        var runTask2TimeSpan = runTask1TimeSpan;
        var runTask2 = SimulateRunWithResourceIdListAndLog(
            logRecordQueue: logRecordQueue,
            resourceIdList: resourceIdList1,
            runAtomicallyForMultipleResourceIdsFromStaticListProvider:
            runAtomicallyForMultipleResourceIdsFromStaticListProvider,
            simulatedRunTimeSpan: runTask2TimeSpan
        );

        var runTask3TimeSpan = safeToUseTimeUnit * 2;
        var runTask3 = SimulateRunWithResourceIdListAndLog(
            logRecordQueue: logRecordQueue,
            resourceIdList: resourceIdList2,
            runAtomicallyForMultipleResourceIdsFromStaticListProvider:
            runAtomicallyForMultipleResourceIdsFromStaticListProvider,
            simulatedRunTimeSpan: runTask3TimeSpan
        );
        var runTask4TimeSpan = runTask3TimeSpan;
        var runTask4 = SimulateRunWithResourceIdListAndLog(
            logRecordQueue: logRecordQueue,
            resourceIdList: resourceIdList2,
            runAtomicallyForMultipleResourceIdsFromStaticListProvider:
            runAtomicallyForMultipleResourceIdsFromStaticListProvider,
            simulatedRunTimeSpan: runTask4TimeSpan
        );

        var runTask5TimeSpan = safeToUseTimeUnit * 3;
        var runTask5 = SimulateRunWithResourceIdListAndLog(
            logRecordQueue: logRecordQueue,
            resourceIdList: resourceIdList3,
            runAtomicallyForMultipleResourceIdsFromStaticListProvider:
            runAtomicallyForMultipleResourceIdsFromStaticListProvider,
            simulatedRunTimeSpan: runTask5TimeSpan
        );
        var runTask6TimeSpan = runTask5TimeSpan;
        var runTask6 = SimulateRunWithResourceIdListAndLog(
            logRecordQueue: logRecordQueue,
            resourceIdList: resourceIdList3,
            runAtomicallyForMultipleResourceIdsFromStaticListProvider:
            runAtomicallyForMultipleResourceIdsFromStaticListProvider,
            simulatedRunTimeSpan: runTask6TimeSpan
        );

        var runTask7TimeSpan = runTask3TimeSpan;
        var runTask7 = SimulateRunWithResourceIdListAndLog(
            logRecordQueue: logRecordQueue,
            resourceIdList: resourceIdList1,
            runAtomicallyForMultipleResourceIdsFromStaticListProvider:
            runAtomicallyForMultipleResourceIdsFromStaticListProvider,
            simulatedRunTimeSpan: runTask7TimeSpan
        );
        var runTask8TimeSpan = runTask7TimeSpan;
        var runTask8 = SimulateRunWithResourceIdListAndLog(
            logRecordQueue: logRecordQueue,
            resourceIdList: resourceIdList1,
            runAtomicallyForMultipleResourceIdsFromStaticListProvider:
            runAtomicallyForMultipleResourceIdsFromStaticListProvider,
            simulatedRunTimeSpan: runTask8TimeSpan
        );

        var runTask9TimeSpan = runTask1TimeSpan;
        var runTask9 = SimulateRunWithResourceIdListAndLog(
            logRecordQueue: logRecordQueue,
            resourceIdList: resourceIdList2,
            runAtomicallyForMultipleResourceIdsFromStaticListProvider:
            runAtomicallyForMultipleResourceIdsFromStaticListProvider,
            simulatedRunTimeSpan: runTask9TimeSpan
        );
        var runTask10TimeSpan = runTask9TimeSpan;
        var runTask10 = SimulateRunWithResourceIdListAndLog(
            logRecordQueue: logRecordQueue,
            resourceIdList: resourceIdList2,
            runAtomicallyForMultipleResourceIdsFromStaticListProvider:
            runAtomicallyForMultipleResourceIdsFromStaticListProvider,
            simulatedRunTimeSpan: runTask10TimeSpan
        );

        await Task.WhenAll(
            runTask1,
            runTask2,
            runTask3,
            runTask4,
            runTask5,
            runTask6,
            runTask7,
            runTask8,
            runTask9,
            runTask10
        );

        var logRecordList = logRecordQueue.ToArray();

        const int expectedRecordCount = 20;
        Assert.AreEqual(
            actual: logRecordList.Length,
            expected: expectedRecordCount
        );

        var i = 0;
        var currentLogRecord = logRecordList[i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Start
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList1
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: startTime
        );
        var runTask1StartTime = currentLogRecord.DateTimeOffset;

        currentLogRecord = logRecordList[++i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Start
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList2
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: startTime
        );
        var runTask3StartTime = currentLogRecord.DateTimeOffset;

        currentLogRecord = logRecordList[++i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Finish
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList1
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: runTask1StartTime + runTask1TimeSpan
        );
        var runTask1FinishTime = currentLogRecord.DateTimeOffset;

        currentLogRecord = logRecordList[++i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Start
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList1
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: runTask1FinishTime
        );
        var runTask2StartTime = currentLogRecord.DateTimeOffset;

        currentLogRecord = logRecordList[++i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Finish
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList1
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: runTask2StartTime + runTask2TimeSpan
        );
        var runTask2FinishTime = currentLogRecord.DateTimeOffset;

        currentLogRecord = logRecordList[++i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Finish
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList2
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: runTask3StartTime + runTask3TimeSpan
        );
        var runTask3FinishTime = currentLogRecord.DateTimeOffset;

        currentLogRecord = logRecordList[++i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Start
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList2
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: runTask3FinishTime
        );
        var runTask4StartTime = currentLogRecord.DateTimeOffset;

        currentLogRecord = logRecordList[++i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Finish
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList2
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: runTask4StartTime + runTask4TimeSpan
        );
        var runTask4FinishTime = currentLogRecord.DateTimeOffset;

        currentLogRecord = logRecordList[++i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Start
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList3
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: new [] {
                runTask2FinishTime,
                runTask4FinishTime
            }
                .Max()
        );
        var runTask5StartTime = currentLogRecord.DateTimeOffset;

        currentLogRecord = logRecordList[++i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Finish
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList3
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: runTask5StartTime + runTask5TimeSpan
        );
        var runTask5FinishTime = currentLogRecord.DateTimeOffset;

        currentLogRecord = logRecordList[++i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Start
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList3
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: runTask5FinishTime
        );
        var runTask6StartTime = currentLogRecord.DateTimeOffset;

        currentLogRecord = logRecordList[++i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Finish
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList3
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: runTask6StartTime + runTask6TimeSpan
        );
        var runTask6FinishTime = currentLogRecord.DateTimeOffset;

        currentLogRecord = logRecordList[++i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Start
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList1
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: runTask6FinishTime
        );
        var runTask7StartTime = currentLogRecord.DateTimeOffset;

        currentLogRecord = logRecordList[++i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Start
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList2
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: runTask6FinishTime
        );
        var runTask9StartTime = currentLogRecord.DateTimeOffset;

        currentLogRecord = logRecordList[++i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Finish
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList2
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: runTask9StartTime + runTask9TimeSpan
        );
        var runTask9FinishTime = currentLogRecord.DateTimeOffset;

        currentLogRecord = logRecordList[++i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Start
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList2
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: runTask9FinishTime
        );
        var runTask10StartTime = currentLogRecord.DateTimeOffset;

        currentLogRecord = logRecordList[++i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Finish
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList1
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: runTask7StartTime + runTask7TimeSpan
        );
        var runTask7FinishTime = currentLogRecord.DateTimeOffset;

        currentLogRecord = logRecordList[++i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Start
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList1
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: runTask7StartTime + runTask7TimeSpan
        );
        var runTask8StartTime = currentLogRecord.DateTimeOffset;

        currentLogRecord = logRecordList[++i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Finish
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList2
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: runTask10StartTime + runTask10TimeSpan
        );
        var runTask10FinishTime = currentLogRecord.DateTimeOffset;

        currentLogRecord = logRecordList[++i];
        Assert.AreEqual(
            actual: currentLogRecord.LogRecordTypeEnum,
            expected: LogRecordTypeEnum.Finish
        );
        CollectionAssert.AreEqual(
            actual: currentLogRecord.ResourceIdList,
            expected: resourceIdList1
        );
        AssertTimeUtil.AssertTimeWithAllowedError(
            actualTime: currentLogRecord.DateTimeOffset,
            allowedToBeLaterByTimeError: safeToUseTimeUnit,
            expectedTime: runTask8StartTime + runTask8TimeSpan
        );
        var runTask8FinishTime = currentLogRecord.DateTimeOffset;

        var assertedRecordCount = ++i;

        Assert.AreEqual(
            actual: assertedRecordCount,
            expected: expectedRecordCount
        );
    }

    private Task SimulateRunWithResourceIdListAndLog(
        ConcurrentQueue<LogRecord<int>> logRecordQueue,
        int[] resourceIdList,
        IRunAtomicallyForMultipleResourceIdsProvider<int> runAtomicallyForMultipleResourceIdsFromStaticListProvider,
        TimeSpan simulatedRunTimeSpan
    )
    {
        return runAtomicallyForMultipleResourceIdsFromStaticListProvider
            .RunAtomicallyForMultipleResourceIdsAndGetAwaiter(
                asyncAction: async () =>
                {
                    logRecordQueue.Enqueue(
                        item: LogRecord<int>.CreateStartRecord(
                            resourceIdList: resourceIdList
                        )
                    );

                    await RunAfterProvider.RunInOrAfter(
                        scheduler: Scheduler.Default,
                        timeSpan: simulatedRunTimeSpan
                    );

                    logRecordQueue.Enqueue(
                        item: LogRecord<int>.CreateFinishRecord(
                            resourceIdList: resourceIdList
                        )
                    );
                },
                uniqueResourceIdList: resourceIdList
            );
    }
}