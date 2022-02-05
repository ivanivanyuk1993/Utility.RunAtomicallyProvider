using BenchmarkDotNet.Attributes;
using RunAtomicallyForMultipleResourceIdsFromStaticListProviderNS;
using RunAtomicallyProviderNS;
using RunWithMaxConcurrencyProviderNS;

namespace RunAtomicallyForMultipleResourceIdsFromStaticListProviderBenchmarkNS;

public class RunAtomicallyForMultipleResourceIdsFromStaticListProviderBenchmark
{
    public static int[] NSource => new[]
    {
        (int) 1e3,
        (int) 1e4
    };

    public static int[] ResourceIdCountSource => new[]
    {
        (int) 1e0,
        (int) 1e1
    };

    public static int[] SpinCountSource => new[]
    {
        (int) 1e0,
        (int) 1e1,
        (int) 1e2
    };

    [ParamsSource(name: nameof(NSource))] public int N { get; set; }

    [ParamsSource(name: nameof(ResourceIdCountSource))]
    public int ResourceIdCount { get; set; }

    [ParamsSource(name: nameof(SpinCountSource))]
    public int SpinCount { get; set; }

    [Benchmark]
    public void RunAtomically()
    {
        var runAtomicallyProvider = new RunAtomicallyProvider();

        RunWithMaxConcurrencyProvider.RunWithMaxConcurrency(action: () =>
        {
            for (var i = 0; i < N; i++)
            for (var resourceId = 0; resourceId < ResourceIdCount; resourceId++)
                runAtomicallyProvider.RunAtomically(action: SpinWait);
        });
    }

    [Benchmark]
    public async Task RunAtomicallyForMultipleResourceIdsAndGetAwaiter()
    {
        var resourceIdList = Enumerable
            .Range(
                start: 0,
                count: ResourceIdCount
            )
            .ToArray();

        var runAtomicallyForMultipleResourceIdsFromStaticListProvider =
            new RunAtomicallyForMultipleResourceIdsFromStaticListProvider<int>(
                fullResourceIdList: resourceIdList
            );

        await RunWithMaxConcurrencyProvider.RunWithMaxConcurrency(asyncAction: async () =>
        {
            for (var i = 0; i < N; i++)
                await Task.WhenAll(
                    tasks: resourceIdList
                        .Select(selector: resourceId =>
                            runAtomicallyForMultipleResourceIdsFromStaticListProvider
                                .RunAtomicallyForMultipleResourceIdsAndGetAwaiter(
                                    action: SpinWait,
                                    uniqueResourceIdList: new[] {resourceId}
                                )
                        )
                );
        });
    }

    [Benchmark]
    public void RunUnderLock()
    {
        var lockGate = new object();

        RunWithMaxConcurrencyProvider.RunWithMaxConcurrency(action: () =>
        {
            for (var i = 0; i < N; i++)
            for (var resourceId = 0; resourceId < ResourceIdCount; resourceId++)
                lock (lockGate)
                {
                    SpinWait();
                }
        });
    }

    private void SpinWait()
    {
        Thread.SpinWait(iterations: SpinCount);
    }
}