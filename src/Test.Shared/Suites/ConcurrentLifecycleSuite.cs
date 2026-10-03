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
    /// Verifies Clear, Dispose, and read operations racing with concurrent producers and consumers.
    /// </summary>
    public static class ConcurrentLifecycleSuite
    {
        /// <summary>
        /// The suite identifier.
        /// </summary>
        public const string SuiteId = "ConcurrentLifecycle";

        /// <summary>
        /// Creates the suite descriptor.
        /// </summary>
        /// <returns>The suite descriptor.</returns>
        public static TestSuiteDescriptor Create()
        {
            return new TestSuiteDescriptor(
                suiteId: SuiteId,
                displayName: "Concurrent Lifecycle",
                cases: new List<TestCaseDescriptor>
                {
                    TestCases.Async(SuiteId, "DisposeDuringProducersOnlyThrowsObjectDisposed", "Dispose racing producers yields only ObjectDisposedException and disposes every accepted item once", async ct =>
                    {
                        const int producerCount = 4;
                        SelectorQueue<DisposableTracker> queue = SelectorQueue
                            .OrderBy<DisposableTracker, int>(x => x.Priority)
                            .Build();

                        ConcurrentBag<DisposableTracker> accepted = new ConcurrentBag<DisposableTracker>();
                        ConcurrentBag<DisposableTracker> rejected = new ConcurrentBag<DisposableTracker>();
                        ConcurrentBag<Exception> unexpected = new ConcurrentBag<Exception>();
                        using ManualResetEventSlim started = new ManualResetEventSlim(false);
                        int startedCount = 0;

                        List<Task> producers = new List<Task>();
                        for (int producer = 0; producer < producerCount; producer++)
                        {
                            int capturedProducer = producer;
                            producers.Add(Task.Run(() =>
                            {
                                if (Interlocked.Increment(ref startedCount) == producerCount) started.Set();
                                for (int i = 0; i < 100000; i++)
                                {
                                    DisposableTracker tracker = new DisposableTracker(capturedProducer + "-" + i) { Priority = i % 17 };
                                    try
                                    {
                                        queue.Enqueue(tracker);
                                        accepted.Add(tracker);
                                    }
                                    catch (ObjectDisposedException)
                                    {
                                        rejected.Add(tracker);
                                        return;
                                    }
                                    catch (Exception exception)
                                    {
                                        unexpected.Add(exception);
                                        return;
                                    }
                                }
                            }));
                        }

                        started.Wait(ct);
                        await Task.Delay(5, ct).ConfigureAwait(false);
                        queue.Dispose();
                        await Task.WhenAll(producers).ConfigureAwait(false);

                        TestAssert.Equal(0, unexpected.Count, "Unexpected exceptions");
                        foreach (DisposableTracker tracker in accepted)
                        {
                            TestAssert.Equal(1, tracker.DisposeCount, "Accepted tracker " + tracker.Name);
                        }

                        foreach (DisposableTracker tracker in rejected)
                        {
                            TestAssert.Equal(0, tracker.DisposeCount, "Rejected tracker " + tracker.Name);
                        }
                    }),

                    TestCases.Async(SuiteId, "ConcurrentDisposeCallsDisposeItemsOnce", "Concurrent Dispose calls dispose each queued item exactly once", async ct =>
                    {
                        const int itemCount = 5000;
                        SelectorQueue<DisposableTracker> queue = SelectorQueue.Create<DisposableTracker>();
                        List<DisposableTracker> trackers = new List<DisposableTracker>(itemCount);
                        for (int i = 0; i < itemCount; i++)
                        {
                            DisposableTracker tracker = new DisposableTracker("t" + i);
                            trackers.Add(tracker);
                            queue.Enqueue(tracker);
                        }

                        List<Task> disposers = new List<Task>();
                        for (int i = 0; i < 8; i++)
                        {
                            disposers.Add(Task.Run(() => queue.Dispose(), ct));
                        }

                        await Task.WhenAll(disposers).ConfigureAwait(false);

                        foreach (DisposableTracker tracker in trackers)
                        {
                            TestAssert.Equal(1, tracker.DisposeCount, "Tracker " + tracker.Name);
                        }
                    }),

                    TestCases.Async(SuiteId, "ClearRacingProducersAndConsumers", "Clear racing producers and consumers either hands off or disposes each item exactly once", async ct =>
                    {
                        const int producerCount = 4;
                        const int itemsPerProducer = 2000;
                        SelectorQueue<DisposableTracker> queue = SelectorQueue
                            .OrderBy<DisposableTracker, int>(x => x.Priority)
                            .Build();

                        ConcurrentBag<DisposableTracker> all = new ConcurrentBag<DisposableTracker>();
                        ConcurrentBag<DisposableTracker> dequeued = new ConcurrentBag<DisposableTracker>();

                        List<Task> producers = new List<Task>();
                        for (int producer = 0; producer < producerCount; producer++)
                        {
                            int capturedProducer = producer;
                            producers.Add(Task.Run(() =>
                            {
                                for (int i = 0; i < itemsPerProducer; i++)
                                {
                                    DisposableTracker tracker = new DisposableTracker(capturedProducer + "-" + i) { Priority = i % 31 };
                                    all.Add(tracker);
                                    queue.Enqueue(tracker);
                                }
                            }, ct));
                        }

                        Task producersTask = Task.WhenAll(producers);

                        Task consumer = Task.Run(async () =>
                        {
                            while (!(producersTask.IsCompleted && queue.Count == 0))
                            {
                                if (queue.TryDequeue(out DisposableTracker tracker)) dequeued.Add(tracker);
                                else await Task.Delay(1, ct).ConfigureAwait(false);
                            }
                        }, ct);

                        Task clearer = Task.Run(async () =>
                        {
                            while (!producersTask.IsCompleted)
                            {
                                queue.Clear();
                                await Task.Delay(1, ct).ConfigureAwait(false);
                            }
                        }, ct);

                        await producersTask.ConfigureAwait(false);
                        await clearer.ConfigureAwait(false);
                        await consumer.ConfigureAwait(false);
                        queue.Dispose();

                        HashSet<DisposableTracker> dequeuedSet = new HashSet<DisposableTracker>(dequeued);
                        TestAssert.Equal(dequeued.Count, dequeuedSet.Count, "No duplicate dequeues");
                        TestAssert.Equal(producerCount * itemsPerProducer, all.Count, "All produced");

                        foreach (DisposableTracker tracker in all)
                        {
                            int expected = dequeuedSet.Contains(tracker) ? 0 : 1;
                            TestAssert.Equal(expected, tracker.DisposeCount, "Tracker " + tracker.Name);
                        }
                    }),

                    TestCases.Async(SuiteId, "ConcurrentReadersDuringWrites", "Peek, TryPeek, and Count are consistent while producers and consumers run", async ct =>
                    {
                        const int itemCount = 20000;
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderBy<int, int>(x => x)
                            .Build();

                        ConcurrentBag<Exception> failures = new ConcurrentBag<Exception>();
                        Task producer = Task.Run(() =>
                        {
                            for (int i = 0; i < itemCount; i++) queue.Enqueue(i);
                        }, ct);

                        Task consumer = Task.Run(async () =>
                        {
                            int consumed = 0;
                            while (consumed < itemCount)
                            {
                                if (queue.TryDequeue(out _)) consumed++;
                                else await Task.Yield();
                            }
                        }, ct);

                        List<Task> readers = new List<Task>();
                        for (int r = 0; r < 3; r++)
                        {
                            readers.Add(Task.Run(() =>
                            {
                                while (!consumer.IsCompleted)
                                {
                                    try
                                    {
                                        int count = queue.Count;
                                        if (count < 0 || count > itemCount) failures.Add(new InvalidOperationException("Count out of range: " + count));
                                        if (queue.TryPeek(out int head) && (head < 0 || head >= itemCount))
                                        {
                                            failures.Add(new InvalidOperationException("Head out of range: " + head));
                                        }

                                        try
                                        {
                                            queue.Peek();
                                        }
                                        catch (InvalidOperationException)
                                        {
                                        }
                                    }
                                    catch (Exception exception)
                                    {
                                        failures.Add(exception);
                                        return;
                                    }
                                }
                            }, ct));
                        }

                        await Task.WhenAll(producer, consumer).ConfigureAwait(false);
                        await Task.WhenAll(readers).ConfigureAwait(false);

                        TestAssert.Equal(0, failures.Count, "Reader failures");
                        TestAssert.Equal(0, queue.Count, "Queue drained");
                    }),

                    TestCases.Async(SuiteId, "ConcurrentFifoPreservesPerProducerOrder", "Concurrent FIFO producers keep each producer's items in program order", async ct =>
                    {
                        const int producerCount = 6;
                        const int itemsPerProducer = 2000;
                        SelectorQueue<ThreadSafetyStableItem> queue = SelectorQueue.Create<ThreadSafetyStableItem>();

                        List<Task> producers = new List<Task>();
                        for (int producer = 0; producer < producerCount; producer++)
                        {
                            int capturedProducer = producer;
                            producers.Add(Task.Run(() =>
                            {
                                for (int i = 0; i < itemsPerProducer; i++)
                                {
                                    queue.Enqueue(new ThreadSafetyStableItem(capturedProducer, i));
                                }
                            }, ct));
                        }

                        await Task.WhenAll(producers).ConfigureAwait(false);

                        int[] lastSeen = new int[producerCount];
                        for (int i = 0; i < producerCount; i++) lastSeen[i] = -1;

                        int drained = 0;
                        while (queue.TryDequeue(out ThreadSafetyStableItem item))
                        {
                            TestAssert.Equal(lastSeen[item.ProducerId] + 1, item.SequenceInProducer, "Producer " + item.ProducerId + " sequence");
                            lastSeen[item.ProducerId] = item.SequenceInProducer;
                            drained++;
                        }

                        TestAssert.Equal(producerCount * itemsPerProducer, drained, "Drained count");
                    })
                });
        }
    }
}
