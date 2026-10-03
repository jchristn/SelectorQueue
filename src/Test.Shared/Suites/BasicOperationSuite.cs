namespace Test.Shared.Suites
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using SelectorQueue;
    using Test.Shared.Models;
    using Touchstone.Core;

    /// <summary>
    /// Verifies the core queue operations and basic lifecycle behavior.
    /// </summary>
    public static class BasicOperationSuite
    {
        /// <summary>
        /// The suite identifier.
        /// </summary>
        public const string SuiteId = "BasicOperation";

        /// <summary>
        /// Creates the suite descriptor.
        /// </summary>
        /// <returns>The suite descriptor.</returns>
        public static TestSuiteDescriptor Create()
        {
            return new TestSuiteDescriptor(
                suiteId: SuiteId,
                displayName: "Basic Operations",
                cases: new List<TestCaseDescriptor>
                {
                    TestCases.Sync(SuiteId, "CreateWithoutOrderingBehavesAsFIFO", "Create without ordering behaves as FIFO", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue.Create<int>();
                        queue.Enqueue(3);
                        queue.Enqueue(1);
                        queue.Enqueue(2);

                        TestAssert.Equal(3, queue.Dequeue());
                        TestAssert.Equal(1, queue.Dequeue());
                        TestAssert.Equal(2, queue.Dequeue());
                        TestAssert.Equal(0, queue.Count, "Count after drain");
                    }),

                    TestCases.Sync(SuiteId, "OrderedEnqueueAndDequeueUseConfiguredSortOrder", "Ordered enqueue and dequeue use configured sort order", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderBy<int, int>(x => x)
                            .Build();

                        queue.Enqueue(30);
                        queue.Enqueue(10);
                        queue.Enqueue(20);

                        TestAssert.Equal(10, queue.Peek(), "Peek");
                        TestAssert.Equal(10, queue.Dequeue(), "First");
                        TestAssert.Equal(20, queue.Dequeue(), "Second");
                        TestAssert.Equal(30, queue.Dequeue(), "Third");
                    }),

                    TestCases.Sync(SuiteId, "TryPeekAndTryDequeueReportEmptyQueueCorrectly", "TryPeek and TryDequeue report empty queue correctly", () =>
                    {
                        SelectorQueue<string> queue = SelectorQueue.Create<string>();

                        TestAssert.False(queue.TryPeek(out string peeked), "TryPeek empty");
                        TestAssert.True(peeked == null, "Empty TryPeek default");
                        TestAssert.False(queue.TryDequeue(out string dequeued), "TryDequeue empty");
                        TestAssert.True(dequeued == null, "Empty TryDequeue default");
                    }),

                    TestCases.Sync(SuiteId, "DequeueAndPeekThrowOnEmptyQueue", "Dequeue and Peek throw on empty queue", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue.Create<int>();
                        TestAssert.Throws<InvalidOperationException>(() => queue.Dequeue(), "Dequeue empty");
                        TestAssert.Throws<InvalidOperationException>(() => queue.Peek(), "Peek empty");
                    }),

                    TestCases.Sync(SuiteId, "ClearRemovesItemsAndPreservesOrderingForLaterUse", "Clear removes items and preserves ordering for later use", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderBy<int, int>(x => x)
                            .Build();

                        queue.Enqueue(4);
                        queue.Enqueue(1);
                        queue.Clear();
                        TestAssert.Equal(0, queue.Count, "Count after clear");

                        queue.Enqueue(9);
                        queue.Enqueue(3);

                        TestAssert.Equal(3, queue.Dequeue());
                        TestAssert.Equal(9, queue.Dequeue());
                    }),

                    TestCases.Sync(SuiteId, "CountRemainsAccurateDuringMixedOperations", "Count remains accurate during mixed operations", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderBy<int, int>(x => x)
                            .Build();

                        for (int i = 0; i < 100; i++)
                        {
                            queue.Enqueue(i);
                        }

                        TestAssert.Equal(100, queue.Count, "Count after enqueue");

                        for (int i = 0; i < 40; i++)
                        {
                            queue.Dequeue();
                        }

                        TestAssert.Equal(60, queue.Count, "Count after dequeue");

                        queue.Clear();
                        TestAssert.Equal(0, queue.Count, "Count after clear");
                    }),

                    TestCases.Sync(SuiteId, "DisposeReleasesQueuedDisposableItems", "Dispose releases queued disposable items", () =>
                    {
                        SelectorQueue<DisposableTracker> queue = SelectorQueue
                            .OrderBy<DisposableTracker, string>(x => x.Name)
                            .Build();

                        DisposableTracker first = new DisposableTracker("b");
                        DisposableTracker second = new DisposableTracker("a");

                        queue.Enqueue(first);
                        queue.Enqueue(second);
                        queue.Dispose();

                        TestAssert.Equal(1, first.DisposeCount, "First dispose count");
                        TestAssert.Equal(1, second.DisposeCount, "Second dispose count");
                    }),

                    TestCases.Sync(SuiteId, "DisposedQueueThrowsObjectDisposedExceptionOnOperations", "Disposed queue throws ObjectDisposedException on operations", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderBy<int, int>(x => x)
                            .Build();

                        queue.Dispose();

                        TestAssert.Throws<ObjectDisposedException>(() => queue.Enqueue(1), "Enqueue disposed");
                        TestAssert.Throws<ObjectDisposedException>(() => queue.Dequeue(), "Dequeue disposed");
                        TestAssert.Throws<ObjectDisposedException>(() => queue.Peek(), "Peek disposed");
                        TestAssert.Throws<ObjectDisposedException>(() => queue.Clear(), "Clear disposed");
                        TestAssert.Throws<ObjectDisposedException>(() =>
                        {
                            int count = queue.Count;
                        }, "Count disposed");
                    }),

                    TestCases.Sync(SuiteId, "NewQueueIsEmpty", "Newly created queues report zero items", () =>
                    {
                        TestAssert.Equal(0, SelectorQueue.Create<int>().Count, "FIFO queue");
                        TestAssert.Equal(0, SelectorQueue.OrderBy<int, int>(x => x).Build().Count, "Ordered queue");
                        TestAssert.Equal(0, new SelectorQueueBuilder<int>().Build().Count, "Builder queue");
                    }),

                    TestCases.Sync(SuiteId, "PeekDoesNotRemove", "Peek and TryPeek return the head without removing it", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderBy<int, int>(x => x)
                            .Build();

                        queue.Enqueue(5);
                        queue.Enqueue(2);

                        TestAssert.Equal(2, queue.Peek(), "First Peek");
                        TestAssert.Equal(2, queue.Peek(), "Second Peek");
                        TestAssert.True(queue.TryPeek(out int peeked), "TryPeek result");
                        TestAssert.Equal(2, peeked, "TryPeek value");
                        TestAssert.Equal(2, queue.Count, "Count unchanged");
                    }),

                    TestCases.Sync(SuiteId, "TryDequeueReturnsItemsInOrder", "TryDequeue returns true with items in order, then false when empty", () =>
                    {
                        SelectorQueue<string> queue = SelectorQueue
                            .OrderByDescending<string, int>(x => x.Length)
                            .Build();

                        queue.Enqueue("a");
                        queue.Enqueue("ccc");
                        queue.Enqueue("bb");

                        List<string> drained = new List<string>();
                        while (queue.TryDequeue(out string item)) drained.Add(item);

                        TestAssert.SequenceEqual(new List<string> { "ccc", "bb", "a" }, drained, "Drain order");
                        TestAssert.False(queue.TryDequeue(out string after), "TryDequeue after drain");
                        TestAssert.Null(after, "Default after drain");
                    }),

                    TestCases.Sync(SuiteId, "DefaultValueItemsAreDistinguishable", "Default-valued items are returned with a true result, unlike an empty queue", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue.Create<int>();
                        queue.Enqueue(0);

                        TestAssert.True(queue.TryPeek(out int peeked), "TryPeek with zero item");
                        TestAssert.Equal(0, peeked, "Peeked zero");
                        TestAssert.True(queue.TryDequeue(out int dequeued), "TryDequeue with zero item");
                        TestAssert.Equal(0, dequeued, "Dequeued zero");
                        TestAssert.False(queue.TryDequeue(out _), "Empty TryDequeue");
                    }),

                    TestCases.Sync(SuiteId, "QueueReusableAfterDrain", "Queue accepts and orders new items after being fully drained", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderBy<int, int>(x => x)
                            .Build();

                        queue.Enqueue(1);
                        queue.Dequeue();
                        TestAssert.Throws<InvalidOperationException>(() => queue.Dequeue(), "Dequeue on drained queue");

                        queue.Enqueue(9);
                        queue.Enqueue(4);
                        TestAssert.Equal(4, queue.Dequeue(), "First after reuse");
                        TestAssert.Equal(9, queue.Dequeue(), "Second after reuse");
                    }),

                    TestCases.Sync(SuiteId, "EmptyQueueExceptionMessage", "Empty Dequeue and Peek report a meaningful message", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue.Create<int>();
                        InvalidOperationException dequeue = TestAssert.Throws<InvalidOperationException>(() => queue.Dequeue());
                        InvalidOperationException peek = TestAssert.Throws<InvalidOperationException>(() => queue.Peek());
                        TestAssert.True(dequeue.Message.Contains("empty"), "Dequeue message '" + dequeue.Message + "'");
                        TestAssert.True(peek.Message.Contains("empty"), "Peek message '" + peek.Message + "'");
                    }),

                    TestCases.Sync(SuiteId, "ValueTypeItems", "Struct items are stored and returned by value", () =>
                    {
                        SelectorQueue<DateTime> queue = SelectorQueue
                            .OrderByDescending<DateTime, DateTime>(x => x)
                            .Build();

                        DateTime early = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        DateTime late = early.AddDays(1);
                        queue.Enqueue(early);
                        queue.Enqueue(late);

                        TestAssert.Equal(late, queue.Dequeue(), "Latest first");
                        TestAssert.Equal(early, queue.Dequeue(), "Earliest second");
                    })
                });
        }

    }
}
