namespace Test.Automated.Suites
{
    using System.Collections.Concurrent;
    using System.Threading;
    using SelectorQueue;

    /// <summary>
    /// Verifies thread-safety invariants under concurrent producer and consumer workloads.
    /// </summary>
    public class ThreadSafetyTests : TestSuite
    {
        /// <summary>
        /// Gets the display name of this test suite.
        /// </summary>
        public override string Name => "Thread Safety";

        /// <summary>
        /// Executes the tests in this suite.
        /// </summary>
        /// <returns>A task that completes when all tests have run.</returns>
        protected override async Task RunTestsAsync()
        {
            await RunTest("Concurrent producers preserve all items and final ordering", async () =>
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
                AssertEqual(itemsPerProducer * producerCount, queue.Count, "Total count");

                int previous = int.MinValue;
                int drained = 0;
                while (queue.TryDequeue(out int value))
                {
                    AssertGreaterThanOrEqual(value, previous, "Sorted drain");
                    previous = value;
                    drained++;
                }

                AssertEqual(itemsPerProducer * producerCount, drained, "Drained count");
            });

            await RunTest("Concurrent consumers drain each enqueued item exactly once", async () =>
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
                            AssertTrue(seen.TryAdd(value, 0), "Duplicate dequeue " + value);
                            Interlocked.Increment(ref dequeued);
                        }
                    }));
                }

                await Task.WhenAll(consumers).ConfigureAwait(false);
                AssertEqual(totalItems, dequeued, "Dequeued count");
                AssertEqual(totalItems, seen.Count, "Unique values");
                AssertEqual(0, queue.Count, "Queue empty");
            });

            await RunTest("Mixed producers and consumers account for every item", async () =>
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

                AssertEqual(totalItems, consumedCount, "Consumed count");
                AssertEqual(0, queue.Count, "Queue empty after mixed workload");
            });

            await RunTest("Concurrent equal-key enqueue preserves per-producer program order", async () =>
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
                    AssertGreaterThan(item.SequenceInProducer, lastSeenSequence[item.ProducerId],
                        "Producer " + item.ProducerId + " sequence order");

                    lastSeenSequence[item.ProducerId] = item.SequenceInProducer;
                    drained++;
                }

                AssertEqual(producerCount * itemsPerProducer, drained, "Drained count");
            });
        }

    }
}
