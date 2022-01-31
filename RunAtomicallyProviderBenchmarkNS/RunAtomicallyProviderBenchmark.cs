using BenchmarkDotNet.Attributes;
using RunAtomicallyProviderNS;
using ShardedQueueBenchmarkNS;

namespace RunAtomicallyProviderBenchmarkNS;

/// <summary>
///     Notice that, while numbers may seem too close to bother with supporting <see cref="RunAtomicallyProvider" />,
///     it actually is much more efficient, than `lock`
///     - It doesn't waste other processor time on spinning,
///     it runs actions on first processor, which enters logic, and frees other processors immediately
///     - It supports working with different <see cref="IScheduler" />-s(like most efficient <see cref="EventLoopGroupScheduler"/>)
///     without depending on spawning/blocking of <see cref="Thread"/>-s(which take lots of memory and using more
///     <see cref="Thread"/>-s than cores is less efficient than using <see cref="EventLoopGroupScheduler"/> with
///     <see cref="Thread"/> per processor and no context switching)
///     - It is reasonably(we see tendency in benchmarks) expected to outperform `lock`,
///     when there is high contention, like when operations take longer, or there are more cores
/// </summary>
public class RunAtomicallyProviderBenchmark
{
    public static int[] NSource => new[]
    {
        (int) 1e3,
        (int) 1e4
    };

    public static int[] SpinCountSource => new[]
    {
        (int) 1e0,
        (int) 1e1,
        (int) 1e2
    };

    [ParamsSource(nameof(NSource))] public int N { get; set; }
    [ParamsSource(nameof(SpinCountSource))] public int SpinCount { get; set; }

    [Benchmark]
    public void RunAtomically()
    {
        var runAtomicallyProvider = new RunAtomicallyProvider();

        RunWithMaxConcurrencyProvider.RunWithMaxConcurrency(() =>
        {
            for (var i = 0; i < N; i++)
            {
                runAtomicallyProvider.RunAtomically(action: SpinWait);
            }
        });
    }

    [Benchmark]
    public void RunUnderLock()
    {
        var lockGate = new object();

        RunWithMaxConcurrencyProvider.RunWithMaxConcurrency(() =>
        {
            for (var i = 0; i < N; i++)
            {
                lock (lockGate)
                {
                    SpinWait();
                }
            }
        });
    }

    private void SpinWait()
    {
        Thread.SpinWait(iterations: SpinCount);
    }
}