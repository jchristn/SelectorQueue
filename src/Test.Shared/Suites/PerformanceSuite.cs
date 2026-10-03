namespace Test.Shared.Suites
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using SelectorQueue;
    using Test.Shared.Models;
    using Touchstone.Core;

    /// <summary>
    /// Runs throughput-oriented smoke tests for queue operations.
    /// </summary>
    public static class PerformanceSuite
    {
        /// <summary>
        /// The suite identifier.
        /// </summary>
        public const string SuiteId = "Performance";

        /// <summary>
        /// Creates the suite descriptor.
        /// </summary>
        /// <returns>The suite descriptor.</returns>
        public static TestSuiteDescriptor Create()
        {
            return new TestSuiteDescriptor(
                suiteId: SuiteId,
                displayName: "Performance",
                cases: new List<TestCaseDescriptor>
                {
                    TestCases.Sync(SuiteId, "OrderedEnqueueBenchmarkProfile", "Ordered enqueue benchmark profile", () =>
                    {
                        List<long> samples = MeasureOrderedEnqueueSamples(itemCount: 100000, sampleCount: 6, reverseInput: true, lowCardinality: false);
                        TestAssert.LessThanOrEqual(samples[samples.Count - 1], 10000L, "Ordered enqueue profile p95-ish");
                    }),

                    TestCases.Sync(SuiteId, "OrderedEnqueueThroughputSmoke", "Ordered enqueue throughput smoke", () =>
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

                        TestAssert.LessThanOrEqual(elapsedMs, 10000L, "Ordered enqueue elapsed");
                        TestAssert.Equal(itemCount, queue.Count, "Queue count");
                    }),

                    TestCases.Sync(SuiteId, "OrderedDrainThroughputSmoke", "Ordered drain throughput smoke", () =>
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
                            TestAssert.GreaterThanOrEqual(value, previous, "Drain order");
                            previous = value;
                            drained++;
                        }

                        stopwatch.Stop();
                        long elapsedMs = stopwatch.ElapsedMilliseconds;

                        TestAssert.LessThanOrEqual(elapsedMs, 10000L, "Ordered drain elapsed");
                        TestAssert.Equal(itemCount, drained, "Drained count");
                    }),

                    TestCases.Sync(SuiteId, "FIFOThroughputSmoke", "FIFO throughput smoke", () =>
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
                            TestAssert.Equal(i, queue.Dequeue(), "FIFO order");
                        }

                        stopwatch.Stop();
                        long elapsedMs = stopwatch.ElapsedMilliseconds;

                        TestAssert.LessThanOrEqual(elapsedMs, 10000L, "FIFO round trip elapsed");
                    }),

                    TestCases.Async(SuiteId, "ConcurrentThroughputSmoke", "Concurrent throughput smoke", async ct =>
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

                        Task producersTask = Task.WhenAll(producers);

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

                                    if (producersTask.IsCompleted && queue.Count == 0)
                                    {
                                        return;
                                    }

                                    await Task.Delay(1, ct).ConfigureAwait(false);
                                }
                            }));
                        }

                        await Task.WhenAll(producers).ConfigureAwait(false);
                        await Task.WhenAll(consumers).ConfigureAwait(false);
                        stopwatch.Stop();

                        long elapsedMs = stopwatch.ElapsedMilliseconds;

                        TestAssert.Equal(totalItems, consumedCount, "Consumed count");
                        TestAssert.Equal(0, queue.Count, "Queue empty");
                        TestAssert.LessThanOrEqual(elapsedMs, 15000L, "Concurrent throughput elapsed");
                    }),

                    TestCases.Sync(SuiteId, "LowCardinalityOrderedEnqueueThroughputSmoke", "Low-cardinality ordered enqueue throughput smoke", () =>
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

                        int previous = int.MinValue;
                        while (queue.TryDequeue(out int value))
                        {
                            TestAssert.GreaterThanOrEqual(value, previous, "Low-cardinality order");
                            previous = value;
                        }

                        TestAssert.LessThanOrEqual(elapsedMs, 10000L, "Low-cardinality enqueue elapsed");
                    }),

                    TestCases.Sync(SuiteId, "OrderedWorkloadAllocationSmoke", "Ordered workload allocation smoke", () =>
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

                        TestAssert.LessThanOrEqual(allocatedBytes, 128000000L, "Allocated bytes");
                    }),

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
    }
}

