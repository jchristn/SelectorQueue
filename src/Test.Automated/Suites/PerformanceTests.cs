namespace Test.Automated.Suites
{
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading;
    using SelectorQueue;

    /// <summary>
    /// Runs throughput-oriented smoke tests for queue operations.
    /// </summary>
    public class PerformanceTests : TestSuite
    {
        /// <summary>
        /// Gets the display name of this test suite.
        /// </summary>
        public override string Name => "Performance";

        /// <summary>
        /// Executes the tests in this suite.
        /// </summary>
        /// <returns>A task that completes when all tests have run.</returns>
        protected override async Task RunTestsAsync()
        {
            await RunTest("Ordered enqueue benchmark profile", () =>
            {
                List<long> samples = MeasureOrderedEnqueueSamples(itemCount: 100000, sampleCount: 6, reverseInput: true, lowCardinality: false);
                PrintSampleSummary("Ordered enqueue profile", 100000, samples);
                AssertLessThanOrEqual(samples[samples.Count - 1], 10000L, "Ordered enqueue profile p95-ish");
            });

            await RunTest("Ordered enqueue throughput smoke", () =>
            {
                const int itemCount = 200000;
                SelectorQueue<int> queue = SelectorQueue
                    .OrderBy<int, int>(x => x)
                    .Build();

                Stopwatch stopwatch = Stopwatch.StartNew();
                for (int i = itemCount - 1; i >= 0; i--)
                {
                    queue.Enqueue(i);
                }

                stopwatch.Stop();
                long elapsedMs = stopwatch.ElapsedMilliseconds;
                double opsPerSecond = itemCount / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);
                Console.WriteLine("         Ordered enqueue: " + itemCount + " items in " + elapsedMs + "ms (" + opsPerSecond.ToString("F0") + " ops/s)");

                AssertLessThanOrEqual(elapsedMs, 10000L, "Ordered enqueue elapsed");
                AssertEqual(itemCount, queue.Count, "Queue count");
            });

            await RunTest("Ordered drain throughput smoke", () =>
            {
                const int itemCount = 200000;
                SelectorQueue<int> queue = SelectorQueue
                    .OrderBy<int, int>(x => x)
                    .Build();

                for (int i = itemCount - 1; i >= 0; i--)
                {
                    queue.Enqueue(i);
                }

                Stopwatch stopwatch = Stopwatch.StartNew();
                int previous = int.MinValue;
                int drained = 0;
                while (queue.TryDequeue(out int value))
                {
                    AssertGreaterThanOrEqual(value, previous, "Drain order");
                    previous = value;
                    drained++;
                }

                stopwatch.Stop();
                long elapsedMs = stopwatch.ElapsedMilliseconds;
                double opsPerSecond = drained / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);
                Console.WriteLine("         Ordered drain: " + drained + " items in " + elapsedMs + "ms (" + opsPerSecond.ToString("F0") + " ops/s)");

                AssertLessThanOrEqual(elapsedMs, 10000L, "Ordered drain elapsed");
                AssertEqual(itemCount, drained, "Drained count");
            });

            await RunTest("FIFO throughput smoke", () =>
            {
                const int itemCount = 300000;
                SelectorQueue<int> queue = SelectorQueue.Create<int>();

                Stopwatch stopwatch = Stopwatch.StartNew();
                for (int i = 0; i < itemCount; i++)
                {
                    queue.Enqueue(i);
                }

                for (int i = 0; i < itemCount; i++)
                {
                    AssertEqual(i, queue.Dequeue(), "FIFO order");
                }

                stopwatch.Stop();
                long elapsedMs = stopwatch.ElapsedMilliseconds;
                double opsPerSecond = (itemCount * 2.0) / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);
                Console.WriteLine("         FIFO round trip: " + itemCount + " enqueues + dequeues in " + elapsedMs + "ms (" + opsPerSecond.ToString("F0") + " ops/s)");

                AssertLessThanOrEqual(elapsedMs, 10000L, "FIFO round trip elapsed");
            });

            await RunTest("Concurrent throughput smoke", async () =>
            {
                const int producerCount = 4;
                const int consumerCount = 4;
                const int itemsPerProducer = 25000;
                const int totalItems = producerCount * itemsPerProducer;

                SelectorQueue<int> queue = SelectorQueue
                    .OrderBy<int, int>(x => x)
                    .Build();

                int producersCompleted = 0;
                int consumedCount = 0;
                ConcurrentBag<int> consumed = new ConcurrentBag<int>();
                Stopwatch stopwatch = Stopwatch.StartNew();

                List<Task> producers = new List<Task>();
                for (int producerId = 0; producerId < producerCount; producerId++)
                {
                    int capturedProducerId = producerId;
                    producers.Add(Task.Run(() =>
                    {
                        Random random = new Random(5000 + capturedProducerId);
                        for (int i = 0; i < itemsPerProducer; i++)
                        {
                            queue.Enqueue(random.Next());
                        }

                        Interlocked.Increment(ref producersCompleted);
                    }));
                }

                List<Task> consumers = new List<Task>();
                for (int consumerId = 0; consumerId < consumerCount; consumerId++)
                {
                    consumers.Add(Task.Run(async () =>
                    {
                        while (true)
                        {
                            if (queue.TryDequeue(out int value))
                            {
                                consumed.Add(value);
                                if (Interlocked.Increment(ref consumedCount) == totalItems)
                                {
                                    return;
                                }

                                continue;
                            }

                            if (Volatile.Read(ref producersCompleted) == producerCount &&
                                Volatile.Read(ref consumedCount) == totalItems)
                            {
                                return;
                            }

                            await Task.Delay(1).ConfigureAwait(false);
                        }
                    }));
                }

                await Task.WhenAll(producers).ConfigureAwait(false);
                await Task.WhenAll(consumers).ConfigureAwait(false);
                stopwatch.Stop();

                long elapsedMs = stopwatch.ElapsedMilliseconds;
                double opsPerSecond = ((double)totalItems * 2) / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);
                Console.WriteLine("         Concurrent round trip: " + totalItems + " items in " + elapsedMs + "ms (" + opsPerSecond.ToString("F0") + " ops/s)");

                AssertEqual(totalItems, consumedCount, "Consumed count");
                AssertEqual(0, queue.Count, "Queue empty");
                AssertLessThanOrEqual(elapsedMs, 15000L, "Concurrent throughput elapsed");
            });

            await RunTest("Low-cardinality ordered enqueue throughput smoke", () =>
            {
                const int itemCount = 200000;
                SelectorQueue<int> queue = SelectorQueue
                    .OrderBy<int, int>(x => x)
                    .Build();

                Stopwatch stopwatch = Stopwatch.StartNew();
                for (int i = 0; i < itemCount; i++)
                {
                    queue.Enqueue(i % 8);
                }

                stopwatch.Stop();
                long elapsedMs = stopwatch.ElapsedMilliseconds;
                double opsPerSecond = itemCount / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);
                Console.WriteLine("         Low-cardinality enqueue: " + itemCount + " items in " + elapsedMs + "ms (" + opsPerSecond.ToString("F0") + " ops/s)");

                int previous = int.MinValue;
                while (queue.TryDequeue(out int value))
                {
                    AssertGreaterThanOrEqual(value, previous, "Low-cardinality order");
                    previous = value;
                }

                AssertLessThanOrEqual(elapsedMs, 10000L, "Low-cardinality enqueue elapsed");
            });

            await RunTest("Ordered workload allocation smoke", () =>
            {
                const int itemCount = 100000;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                long before = GC.GetTotalAllocatedBytes(true);
                SelectorQueue<int> queue = SelectorQueue
                    .OrderBy<int, int>(x => x)
                    .Build();

                for (int i = itemCount - 1; i >= 0; i--)
                {
                    queue.Enqueue(i);
                }

                while (queue.TryDequeue(out _))
                {
                }

                long after = GC.GetTotalAllocatedBytes(true);
                long allocatedBytes = after - before;

                Console.WriteLine("         Ordered allocation smoke: " + allocatedBytes + " bytes for " + itemCount + " items");
                AssertLessThanOrEqual(allocatedBytes, 128000000L, "Allocated bytes");
            });
        }

        private static List<long> MeasureOrderedEnqueueSamples(int itemCount, int sampleCount, bool reverseInput, bool lowCardinality)
        {
            List<long> samples = new List<long>(sampleCount);

            for (int sample = 0; sample < sampleCount; sample++)
            {
                SelectorQueue<int> queue = SelectorQueue
                    .OrderBy<int, int>(x => x)
                    .Build();

                Stopwatch stopwatch = Stopwatch.StartNew();
                for (int i = 0; i < itemCount; i++)
                {
                    int value = reverseInput ? (itemCount - i) : i;
                    if (lowCardinality)
                    {
                        value = value % 8;
                    }

                    queue.Enqueue(value);
                }

                stopwatch.Stop();
                samples.Add(stopwatch.ElapsedMilliseconds);
            }

            samples.Sort();
            return samples;
        }

        private static void PrintSampleSummary(string label, int itemCount, List<long> samples)
        {
            long median = samples[samples.Count / 2];
            long p95 = samples[(samples.Count * 95) / 100];
            Console.WriteLine("         " + label + ": " + itemCount + " items; median " + median + "ms; p95-ish " + p95 + "ms");
        }
    }
}
