namespace Test.Shared.Suites
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using SelectorQueue;
    using Test.Shared.Models;
    using Touchstone.Core;

    /// <summary>
    /// Runs repeated stress workloads intended to catch rare concurrency issues.
    /// </summary>
    public static class StressSuite
    {
        /// <summary>
        /// The suite identifier.
        /// </summary>
        public const string SuiteId = "Stress";

        /// <summary>
        /// Creates the suite descriptor.
        /// </summary>
        /// <returns>The suite descriptor.</returns>
        public static TestSuiteDescriptor Create()
        {
            return new TestSuiteDescriptor(
                suiteId: SuiteId,
                displayName: "Stress",
                cases: new List<TestCaseDescriptor>
                {
                    TestCases.Async(SuiteId, "RepeatedProducerConsumerIterationsAccountForEveryItemExactlyOnce", "Repeated producer-consumer iterations account for every item exactly once", async ct =>
                    {
                        const int iterationCount = 20;
                        const int producerCount = 4;
                        const int consumerCount = 4;
                        const int itemsPerProducer = 1000;
                        const int totalItems = producerCount * itemsPerProducer;

                        for (int iteration = 0; iteration < iterationCount; iteration++)
                        {
                            SelectorQueue<int> queue = SelectorQueue
                                .OrderBy<int, int>(x => x)
                                .Build();

                            ConcurrentDictionary<int, byte> seen = new ConcurrentDictionary<int, byte>();
                            int producersCompleted = 0;
                            int consumedCount = 0;

                            List<Task> producers = new List<Task>();
                            for (int producer = 0; producer < producerCount; producer++)
                            {
                                int capturedProducer = producer;
                                producers.Add(Task.Run(async () =>
                                {
                                    for (int i = 0; i < itemsPerProducer; i++)
                                    {
                                        int value = (iteration * 1000000) + (capturedProducer * itemsPerProducer) + i;
                                        queue.Enqueue(value);

                                        if ((i % 200) == 0)
                                        {
                                            await Task.Delay(1, ct).ConfigureAwait(false);
                                        }
                                    }

                                    Interlocked.Increment(ref producersCompleted);
                                }));
                            }

                            Task producersTask = Task.WhenAll(producers);

                            List<Task> consumers = new List<Task>();
                            for (int consumer = 0; consumer < consumerCount; consumer++)
                            {
                                consumers.Add(Task.Run(async () =>
                                {
                                    while (true)
                                    {
                                        if (queue.TryDequeue(out int value))
                                        {
                                            TestAssert.True(seen.TryAdd(value, 0), "Duplicate value iteration " + iteration + " value " + value);
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

                            TestAssert.Equal(totalItems, consumedCount, "Consumed count iteration " + iteration);
                            TestAssert.Equal(totalItems, seen.Count, "Unique count iteration " + iteration);
                            TestAssert.Equal(0, queue.Count, "Queue empty iteration " + iteration);
                        }
                    }),

                    TestCases.Async(SuiteId, "RepeatedConcurrentProducerDrainsRemainGloballySorted", "Repeated concurrent producer drains remain globally sorted", async ct =>
                    {
                        const int iterationCount = 25;
                        const int producerCount = 6;
                        const int itemsPerProducer = 750;

                        for (int iteration = 0; iteration < iterationCount; iteration++)
                        {
                            SelectorQueue<int> queue = SelectorQueue
                                .OrderBy<int, int>(x => x)
                                .Build();

                            List<Task> producers = new List<Task>();
                            for (int producer = 0; producer < producerCount; producer++)
                            {
                                int capturedProducer = producer;
                                producers.Add(Task.Run(() =>
                                {
                                    Random random = new Random(12000 + (iteration * 17) + capturedProducer);
                                    for (int i = 0; i < itemsPerProducer; i++)
                                    {
                                        queue.Enqueue(random.Next(0, 10000));
                                    }
                                }));
                            }

                            await Task.WhenAll(producers).ConfigureAwait(false);

                            int previous = int.MinValue;
                            int drained = 0;
                            while (queue.TryDequeue(out int value))
                            {
                                TestAssert.GreaterThanOrEqual(value, previous, "Sorted iteration " + iteration);
                                previous = value;
                                drained++;
                            }

                            TestAssert.Equal(producerCount * itemsPerProducer, drained, "Drained count iteration " + iteration);
                        }
                    }),

                    TestCases.Async(SuiteId, "RepeatedEqualKeyStressPreservesEachProducerSequence", "Repeated equal-key stress preserves each producer sequence", async ct =>
                    {
                        const int iterationCount = 20;
                        const int producerCount = 5;
                        const int itemsPerProducer = 600;

                        for (int iteration = 0; iteration < iterationCount; iteration++)
                        {
                            SelectorQueue<ThreadSafetyStableItem> queue = SelectorQueue
                                .OrderBy<ThreadSafetyStableItem, int>(x => x.Priority)
                                .Build();

                            List<Task> producers = new List<Task>();
                            for (int producer = 0; producer < producerCount; producer++)
                            {
                                int capturedProducer = producer;
                                producers.Add(Task.Run(async () =>
                                {
                                    for (int sequence = 0; sequence < itemsPerProducer; sequence++)
                                    {
                                        queue.Enqueue(new ThreadSafetyStableItem(capturedProducer, sequence));
                                        if ((sequence % 150) == 0)
                                        {
                                            await Task.Delay(1, ct).ConfigureAwait(false);
                                        }
                                    }
                                }));
                            }

                            await Task.WhenAll(producers).ConfigureAwait(false);

                            int[] lastSeen = new int[producerCount];
                            for (int i = 0; i < lastSeen.Length; i++)
                            {
                                lastSeen[i] = -1;
                            }

                            int drained = 0;
                            while (queue.TryDequeue(out ThreadSafetyStableItem item))
                            {
                                TestAssert.GreaterThan(item.SequenceInProducer, lastSeen[item.ProducerId],
                                    "Producer order iteration " + iteration + " producer " + item.ProducerId);
                                lastSeen[item.ProducerId] = item.SequenceInProducer;
                                drained++;
                            }

                            TestAssert.Equal(producerCount * itemsPerProducer, drained, "Equal-key drained count iteration " + iteration);
                        }
                    }),

                });
        }

    }
}
