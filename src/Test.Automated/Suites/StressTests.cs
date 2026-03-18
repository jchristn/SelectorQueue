namespace Test.Automated.Suites
{
    using System.Collections.Concurrent;
    using System.Threading;
    using SelectorQueue;

    /// <summary>
    /// Runs repeated stress workloads intended to catch rare concurrency issues.
    /// </summary>
    public class StressTests : TestSuite
    {
        /// <summary>
        /// Gets the display name of this test suite.
        /// </summary>
        public override string Name => "Stress";

        /// <summary>
        /// Executes the tests in this suite.
        /// </summary>
        /// <returns>A task that completes when all tests have run.</returns>
        protected override async Task RunTestsAsync()
        {
            await RunTest("Repeated producer-consumer iterations account for every item exactly once", async () =>
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
                                    await Task.Delay(1).ConfigureAwait(false);
                                }
                            }

                            Interlocked.Increment(ref producersCompleted);
                        }));
                    }

                    List<Task> consumers = new List<Task>();
                    for (int consumer = 0; consumer < consumerCount; consumer++)
                    {
                        consumers.Add(Task.Run(async () =>
                        {
                            while (true)
                            {
                                if (queue.TryDequeue(out int value))
                                {
                                    AssertTrue(seen.TryAdd(value, 0), "Duplicate value iteration " + iteration + " value " + value);
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

                    AssertEqual(totalItems, consumedCount, "Consumed count iteration " + iteration);
                    AssertEqual(totalItems, seen.Count, "Unique count iteration " + iteration);
                    AssertEqual(0, queue.Count, "Queue empty iteration " + iteration);
                }
            });

            await RunTest("Repeated concurrent producer drains remain globally sorted", async () =>
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
                        AssertGreaterThanOrEqual(value, previous, "Sorted iteration " + iteration);
                        previous = value;
                        drained++;
                    }

                    AssertEqual(producerCount * itemsPerProducer, drained, "Drained count iteration " + iteration);
                }
            });

            await RunTest("Repeated equal-key stress preserves each producer sequence", async () =>
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
                                    await Task.Delay(1).ConfigureAwait(false);
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
                        AssertGreaterThan(item.SequenceInProducer, lastSeen[item.ProducerId],
                            "Producer order iteration " + iteration + " producer " + item.ProducerId);
                        lastSeen[item.ProducerId] = item.SequenceInProducer;
                        drained++;
                    }

                    AssertEqual(producerCount * itemsPerProducer, drained, "Equal-key drained count iteration " + iteration);
                }
            });
        }
    }
}
