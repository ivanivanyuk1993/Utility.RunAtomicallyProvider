``` ini

BenchmarkDotNet=v0.13.1, OS=ubuntu 21.10
AMD Ryzen 9 3900XT, 1 CPU, 24 logical and 12 physical cores
.NET SDK=6.0.101
  [Host]     : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
  DefaultJob : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT


```
|                                           Method |     N | ResourceIdCount | SpinCount |         Mean |       Error |        StdDev |       Median |
|------------------------------------------------- |------ |---------------- |---------- |-------------:|------------:|--------------:|-------------:|
|                                    **RunAtomically** |  **1000** |               **1** |         **1** |     **7.577 ms** |   **0.3290 ms** |     **0.9701 ms** |     **7.649 ms** |
| RunAtomicallyForMultipleResourceIdsAndGetAwaiter |  1000 |               1 |         1 |    24.716 ms |   0.4770 ms |     0.4461 ms |    24.647 ms |
|                                     RunUnderLock |  1000 |               1 |         1 |     3.085 ms |   0.0261 ms |     0.0244 ms |     3.081 ms |
|                                    **RunAtomically** |  **1000** |               **1** |        **10** |    **11.113 ms** |   **0.2223 ms** |     **0.3652 ms** |    **11.078 ms** |
| RunAtomicallyForMultipleResourceIdsAndGetAwaiter |  1000 |               1 |        10 |    31.096 ms |   0.6075 ms |     0.6753 ms |    30.891 ms |
|                                     RunUnderLock |  1000 |               1 |        10 |    12.728 ms |   0.1198 ms |     0.1121 ms |    12.714 ms |
|                                    **RunAtomically** |  **1000** |               **1** |       **100** |    **76.438 ms** |   **1.5189 ms** |     **2.4527 ms** |    **76.759 ms** |
| RunAtomicallyForMultipleResourceIdsAndGetAwaiter |  1000 |               1 |       100 |    95.888 ms |   1.9150 ms |     2.2797 ms |    95.012 ms |
|                                     RunUnderLock |  1000 |               1 |       100 |    89.756 ms |   1.7412 ms |     1.9353 ms |    89.520 ms |
|                                    **RunAtomically** |  **1000** |              **10** |         **1** |    **52.586 ms** |   **1.6827 ms** |     **4.9614 ms** |    **52.666 ms** |
| RunAtomicallyForMultipleResourceIdsAndGetAwaiter |  1000 |              10 |         1 |   168.443 ms |   0.5610 ms |     0.4973 ms |   168.404 ms |
|                                     RunUnderLock |  1000 |              10 |         1 |    22.069 ms |   0.4321 ms |     0.6978 ms |    22.096 ms |
|                                    **RunAtomically** |  **1000** |              **10** |        **10** |   **107.176 ms** |   **2.1698 ms** |     **6.3977 ms** |   **108.331 ms** |
| RunAtomicallyForMultipleResourceIdsAndGetAwaiter |  1000 |              10 |        10 |   203.765 ms |   1.0359 ms |     0.9183 ms |   203.619 ms |
|                                     RunUnderLock |  1000 |              10 |        10 |   102.937 ms |   8.3112 ms |    24.5058 ms |   109.763 ms |
|                                    **RunAtomically** |  **1000** |              **10** |       **100** |   **728.655 ms** |  **13.9804 ms** |    **18.1785 ms** |   **730.575 ms** |
| RunAtomicallyForMultipleResourceIdsAndGetAwaiter |  1000 |              10 |       100 |   905.256 ms |   2.6077 ms |     2.4392 ms |   905.543 ms |
|                                     RunUnderLock |  1000 |              10 |       100 | 1,396.091 ms |  33.8798 ms |    99.8954 ms | 1,417.086 ms |
|                                    **RunAtomically** | **10000** |               **1** |         **1** |    **46.027 ms** |   **0.9194 ms** |     **2.6231 ms** |    **45.873 ms** |
| RunAtomicallyForMultipleResourceIdsAndGetAwaiter | 10000 |               1 |         1 |   239.322 ms |   2.4409 ms |     2.2832 ms |   239.068 ms |
|                                     RunUnderLock | 10000 |               1 |         1 |    20.818 ms |   0.3951 ms |     0.3503 ms |    20.752 ms |
|                                    **RunAtomically** | **10000** |               **1** |        **10** |   **105.189 ms** |   **2.0650 ms** |     **4.0277 ms** |   **104.414 ms** |
| RunAtomicallyForMultipleResourceIdsAndGetAwaiter | 10000 |               1 |        10 |   303.643 ms |   2.4092 ms |     2.2535 ms |   303.141 ms |
|                                     RunUnderLock | 10000 |               1 |        10 |   123.632 ms |   2.8780 ms |     8.4859 ms |   122.811 ms |
|                                    **RunAtomically** | **10000** |               **1** |       **100** |   **752.057 ms** |  **14.7973 ms** |    **25.5246 ms** |   **758.399 ms** |
| RunAtomicallyForMultipleResourceIdsAndGetAwaiter | 10000 |               1 |       100 |   956.610 ms |  21.5551 ms |    63.5556 ms |   968.361 ms |
|                                     RunUnderLock | 10000 |               1 |       100 | 1,025.725 ms | 143.9237 ms |   424.3622 ms |   800.771 ms |
|                                    **RunAtomically** | **10000** |              **10** |         **1** |   **777.738 ms** |  **15.4221 ms** |    **32.5304 ms** |   **780.543 ms** |
| RunAtomicallyForMultipleResourceIdsAndGetAwaiter | 10000 |              10 |         1 | 1,665.510 ms |   6.2794 ms |     5.5665 ms | 1,666.006 ms |
|                                     RunUnderLock | 10000 |              10 |         1 |   292.707 ms |  13.2314 ms |    39.0131 ms |   292.846 ms |
|                                    **RunAtomically** | **10000** |              **10** |        **10** | **1,379.572 ms** |  **26.5940 ms** |    **31.6582 ms** | **1,385.093 ms** |
| RunAtomicallyForMultipleResourceIdsAndGetAwaiter | 10000 |              10 |        10 | 2,279.884 ms |   9.7014 ms |     9.0747 ms | 2,277.572 ms |
|                                     RunUnderLock | 10000 |              10 |        10 | 1,484.236 ms | 132.2932 ms |   390.0695 ms | 1,308.468 ms |
|                                    **RunAtomically** | **10000** |              **10** |       **100** | **6,160.217 ms** | **566.2749 ms** | **1,669.6745 ms** | **7,349.676 ms** |
| RunAtomicallyForMultipleResourceIdsAndGetAwaiter | 10000 |              10 |       100 | 6,210.227 ms | 499.8285 ms | 1,473.7556 ms | 5,351.293 ms |
|                                     RunUnderLock | 10000 |              10 |       100 | 7,917.189 ms | 212.3402 ms |   595.4246 ms | 7,767.243 ms |
