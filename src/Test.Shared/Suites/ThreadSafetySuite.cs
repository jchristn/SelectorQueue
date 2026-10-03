namespace Test.Shared.Suites
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using SelectorQueue;
    using Test.Shared.Models;
    using Touchstone.Core;

    /// <summary>
    /// Verifies thread-safety invariants under concurrent producer and consumer workloads.
    /// </summary>
    public static class ThreadSafetySuite
    {
        /// <summary>
        /// The suite identifier.
        /// </summary>
        public const string SuiteId = "ThreadSafety";

        /// <summary>
        /// Creates the suite descriptor.
        /// </summary>
        /// <returns>The suite descriptor.</returns>
        public static TestSuiteDescriptor Create()
        {
            return new TestSuiteDescriptor(
                suiteId: SuiteId,
                displayName: "Thread Safety",
                cases: new List<TestCaseDescriptor>
                {
                    TestCases.Async(SuiteId, "ConcurrentProducersPreserveAllItemsAndFinalOrdering", "Concurrent producers preserve all items and final ordering", async ct =>
                    {
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderBy<int, int>(x => x)
                            .Build();

                        int itemsPerProducer = 2000;
                        int producerCount = 8;
                        List<Task> producers = new List<Task>();

                        for (int producer = 0; producer < producerCount; producer++)
                        {
                            int producerId = producer;
                            producers.Add(Task.Run(() =>
                            {
                                for (int i = 0; i < itemsPerProducer; i++)
                                {
                                    queue.Enqueue((producerId * itemsPerProducer) + i);
                                }
                            }));
                        }

                        await Task.WhenAll(producers).ConfigureAwait(false);
                        TestAssert.Equal(itemsPerProducer * producerCount, queue.Count, "Total count");

                        int previous = int.MinValue;
                        int drained = 0;
                        while (queue.TryDequeue(out int value))
                        {
                            TestAssert.GreaterThanOrEqual(value, previous, "Sorted drain");
                            previous = value;
                            drained++;
                        }

                        TestAssert.Equal(itemsPerProducer * producerCount, drained, "Drained count");
                    }),

                    TestCases.Async(SuiteId, "ConcurrentConsumersDrainEachEnqueuedItemExactlyOnce", "Concurrent consumers drain each enqueued item exactly once", async ct =>
                    {
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderBy<int, int>(x => x)
                            .Build();

                        const int totalItems = 12000;
                        for (int i = 0; i < totalItems; i++)
                        {
                            queue.Enqueue(i);
                        }

                        ConcurrentDictionary<int, byte> seen = new ConcurrentDictionary<int, byte>();
                        int dequeued = 0;
                        List<Task> consumers = new List<Task>();

                        for (int i = 0; i < 6; i++)
                        {
                            consumers.Add(Task.Run(() =>
                            {
                                while (queue.TryDequeue(out int value))
                                {
                                    TestAssert.True(seen.TryAdd(value, 0), "Duplicate dequeue " + value);
                                    Interlocked.Increment(ref dequeued);
                                }
                            }));
                        }

                        await Task.WhenAll(consumers).ConfigureAwait(false);
                        TestAssert.Equal(totalItems, dequeued, "Dequeued count");
                        TestAssert.Equal(totalItems, seen.Count, "Unique values");
                        TestAssert.Equal(0, queue.Count, "Queue empty");
                    }),

                    TestCases.Async(SuiteId, "MixedProducersAndConsumersAccountForEveryItem", "Mixed producers and consumers account for every item", async ct =>
                    {
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderBy<int, int>(x => x)
                            .Build();

                        const int producerCount = 4;
                        const int itemsPerProducer = 3000;
                        const int totalItems = producerCount * itemsPerProducer;
                        ConcurrentBag<int> consumed = new ConcurrentBag<int>();
                        int producersCompleted = 0;
                        int consumedCount = 0;

                        List<Task> producers = new List<Task>();
                        for (int producer = 0; producer < producerCount; producer++)
                        {
                            int producerId = producer;
                            producers.Add(Task.Run(() =>
                            {
                                Random random = new Random(1000 + producerId);
                                for (int i = 0; i < itemsPerProducer; i++)
                                {
                                    queue.Enqueue(random.Next(0, 50000));
                                }

                                Interlocked.Increment(ref producersCompleted);
                            }));
                        }

                        Task producersTask = Task.WhenAll(producers);

                        List<Task> consumers = new List<Task>();
                        for (int consumer = 0; consumer < 4; consumer++)
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

                        TestAssert.Equal(totalItems, consumedCount, "Consumed count");
                        TestAssert.Equal(0, queue.Count, "Queue empty after mixed workload");
                    }),

                    TestCases.Async(SuiteId, "ConcurrentEqualKeyEnqueuePreservesPerProducerProgramOrder", "Concurrent equal-key enqueue preserves per-producer program order", async ct =>
                    {
                        SelectorQueue<ThreadSafetyStableItem> queue = SelectorQueue
                            .OrderBy<ThreadSafetyStableItem, int>(x => x.Priority)
                            .Build();

                        const int producerCount = 4;
                        const int itemsPerProducer = 1000;
                        List<Task> producers = new List<Task>();

                        for (int producerId = 0; producerId < producerCount; producerId++)
                        {
                            int capturedProducerId = producerId;
                            producers.Add(Task.Run(() =>
                            {
                                for (int sequence = 0; sequence < itemsPerProducer; sequence++)
                                {
                                    queue.Enqueue(new ThreadSafetyStableItem(capturedProducerId, sequence));
                                }
                            }));
                        }

                        await Task.WhenAll(producers).ConfigureAwait(false);

                        int[] lastSeenSequence = Enumerable.Repeat(-1, producerCount).ToArray();
                        int drained = 0;
                        while (queue.TryDequeue(out ThreadSafetyStableItem item))
                        {
                            TestAssert.GreaterThan(item.SequenceInProducer, lastSeenSequence[item.ProducerId],
                                "Producer " + item.ProducerId + " sequence order");

                            lastSeenSequence[item.ProducerId] = item.SequenceInProducer;
                            drained++;
                        }

                        TestAssert.Equal(producerCount * itemsPerProducer, drained, "Drained count");
                    }),

                });
        }

    }
}
