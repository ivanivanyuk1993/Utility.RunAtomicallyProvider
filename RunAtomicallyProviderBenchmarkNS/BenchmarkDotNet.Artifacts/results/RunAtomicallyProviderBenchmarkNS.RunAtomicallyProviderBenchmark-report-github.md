``` ini

BenchmarkDotNet=v0.13.1, OS=ubuntu 21.10
AMD Ryzen 9 3900XT, 1 CPU, 24 logical and 12 physical cores
.NET SDK=6.0.101
  [Host]     : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
  DefaultJob : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT


```
|        Method |     N | SpinCount |         Mean |       Error |      StdDev |       Median |
|-------------- |------ |---------- |-------------:|------------:|------------:|-------------:|
| **RunAtomically** |  **1000** |         **1** |     **7.456 ms** |   **0.3240 ms** |   **0.9554 ms** |     **7.643 ms** |
|  RunUnderLock |  1000 |         1 |     3.119 ms |   0.0191 ms |   0.0179 ms |     3.116 ms |
| **RunAtomically** |  **1000** |        **10** |    **12.834 ms** |   **0.5000 ms** |   **1.4741 ms** |    **12.883 ms** |
|  RunUnderLock |  1000 |        10 |    12.621 ms |   0.1556 ms |   0.1455 ms |    12.650 ms |
| **RunAtomically** |  **1000** |       **100** |    **71.693 ms** |   **4.1609 ms** |  **12.2685 ms** |    **75.813 ms** |
|  RunUnderLock |  1000 |       100 |    87.063 ms |   1.4831 ms |   1.7079 ms |    87.213 ms |
| **RunAtomically** | **10000** |         **1** |    **53.363 ms** |   **1.4744 ms** |   **4.3008 ms** |    **53.770 ms** |
|  RunUnderLock | 10000 |         1 |    20.479 ms |   0.2991 ms |   0.2498 ms |    20.563 ms |
| **RunAtomically** | **10000** |        **10** |   **103.637 ms** |   **2.0689 ms** |   **2.8320 ms** |   **104.309 ms** |
|  RunUnderLock | 10000 |        10 |   118.504 ms |   2.9747 ms |   8.7709 ms |   116.695 ms |
| **RunAtomically** | **10000** |       **100** |   **718.025 ms** |  **13.0177 ms** |  **16.4632 ms** |   **716.033 ms** |
|  RunUnderLock | 10000 |       100 | 1,095.880 ms | 123.5427 ms | 364.2685 ms | 1,241.730 ms |
