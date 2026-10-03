namespace Test.Shared.Suites
{
    using System;
    using System.Collections.Generic;
    using SelectorQueue;
    using Test.Shared.Models;
    using Touchstone.Core;

    /// <summary>
    /// Verifies item ownership, disposal, and post-disposal semantics.
    /// </summary>
    public static class OwnershipSuite
    {
        /// <summary>
        /// The suite identifier.
        /// </summary>
        public const string SuiteId = "Ownership";

        /// <summary>
        /// Creates the suite descriptor.
        /// </summary>
        /// <returns>The suite descriptor.</returns>
        public static TestSuiteDescriptor Create()
        {
            return new TestSuiteDescriptor(
                suiteId: SuiteId,
                displayName: "Ownership and Disposal",
                cases: new List<TestCaseDescriptor>
                {
                    TestCases.Sync(SuiteId, "DisposeIsIdempotent", "Calling Dispose twice does not throw or double-dispose items", () =>
                    {
                        SelectorQueue<DisposableTracker> queue = SelectorQueue.Create<DisposableTracker>();
                        DisposableTracker tracker = new DisposableTracker("a");
                        queue.Enqueue(tracker);

                        queue.Dispose();
                        queue.Dispose();

                        TestAssert.Equal(1, tracker.DisposeCount, "Dispose count");
                    }),

                    TestCases.Sync(SuiteId, "ClearThenDisposeDoesNotRedispose", "Items disposed by Clear are not disposed again by Dispose", () =>
                    {
                        SelectorQueue<DisposableTracker> queue = SelectorQueue.Create<DisposableTracker>();
                        DisposableTracker cleared = new DisposableTracker("cleared");
                        queue.Enqueue(cleared);
                        queue.Clear();

                        DisposableTracker pending = new DisposableTracker("pending");
                        queue.Enqueue(pending);
                        queue.Dispose();

                        TestAssert.Equal(1, cleared.DisposeCount, "Cleared item dispose count");
                        TestAssert.Equal(1, pending.DisposeCount, "Pending item dispose count");
                    }),

                    TestCases.Sync(SuiteId, "TryDequeuedItemNotDisposed", "Item returned by TryDequeue is owned by the caller", () =>
                    {
                        SelectorQueue<DisposableTracker> queue = SelectorQueue
                            .OrderBy<DisposableTracker, string>(x => x.Name)
                            .Build();

                        DisposableTracker taken = new DisposableTracker("a");
                        DisposableTracker left = new DisposableTracker("b");
                        queue.Enqueue(left);
                        queue.Enqueue(taken);

                        TestAssert.True(queue.TryDequeue(out DisposableTracker result), "TryDequeue");
                        queue.Clear();

                        TestAssert.Same(taken, result, "Dequeued instance");
                        TestAssert.Equal(0, taken.DisposeCount, "Taken dispose count");
                        TestAssert.Equal(1, left.DisposeCount, "Left dispose count");
                    }),

                    TestCases.Sync(SuiteId, "PeekedItemRemainsOwnedByQueue", "Peeked items remain owned by the queue and are disposed with it", () =>
                    {
                        SelectorQueue<DisposableTracker> queue = SelectorQueue.Create<DisposableTracker>();
                        DisposableTracker tracker = new DisposableTracker("a");
                        queue.Enqueue(tracker);

                        DisposableTracker peeked = queue.Peek();
                        TestAssert.True(queue.TryPeek(out DisposableTracker tryPeeked), "TryPeek");
                        queue.Dispose();

                        TestAssert.Same(tracker, peeked, "Peek instance");
                        TestAssert.Same(tracker, tryPeeked, "TryPeek instance");
                        TestAssert.Equal(1, tracker.DisposeCount, "Dispose count");
                    }),

                    TestCases.Sync(SuiteId, "MixedDisposableAndPlainItems", "Only IDisposable items are disposed when a queue holds mixed types", () =>
                    {
                        SelectorQueue<object> queue = SelectorQueue.Create<object>();
                        DisposableTracker tracker = new DisposableTracker("a");
                        queue.Enqueue("plain string");
                        queue.Enqueue(tracker);
                        queue.Enqueue(42);

                        queue.Clear();

                        TestAssert.Equal(1, tracker.DisposeCount, "Tracker dispose count");
                        TestAssert.Equal(0, queue.Count, "Count");
                    }),

                    TestCases.Sync(SuiteId, "NullItemsSkippedDuringDisposal", "Null items are skipped safely during Clear and Dispose", () =>
                    {
                        SelectorQueue<DisposableTracker?> queue = SelectorQueue.Create<DisposableTracker?>();
                        DisposableTracker tracker = new DisposableTracker("a");
                        queue.Enqueue(null);
                        queue.Enqueue(tracker);
                        queue.Clear();

                        queue.Enqueue(null);
                        queue.Dispose();

                        TestAssert.Equal(1, tracker.DisposeCount, "Tracker dispose count");
                    }),

                    TestCases.Sync(SuiteId, "SameInstanceEnqueuedTwice", "Same instance enqueued twice is dequeued twice and disposed per entry", () =>
                    {
                        SelectorQueue<DisposableTracker> queue = SelectorQueue.Create<DisposableTracker>();
                        DisposableTracker tracker = new DisposableTracker("a");
                        queue.Enqueue(tracker);
                        queue.Enqueue(tracker);
                        TestAssert.Equal(2, queue.Count, "Count with duplicate entries");

                        TestAssert.Same(tracker, queue.Dequeue(), "First entry");
                        queue.Dispose();

                        TestAssert.Equal(1, tracker.DisposeCount, "Remaining entry disposed once");
                    }),

                    TestCases.Sync(SuiteId, "UsingStatementDisposesQueue", "A using declaration disposes the queue and its items", () =>
                    {
                        DisposableTracker tracker = new DisposableTracker("a");
                        SelectorQueue<DisposableTracker> captured;

                        using (SelectorQueue<DisposableTracker> queue = SelectorQueue.Create<DisposableTracker>())
                        {
                            queue.Enqueue(tracker);
                            captured = queue;
                        }

                        TestAssert.Equal(1, tracker.DisposeCount, "Dispose count");
                        TestAssert.Throws<ObjectDisposedException>(() => captured.Enqueue(tracker), "Enqueue after using");
                    }),

                    TestCases.Sync(SuiteId, "DisposedTryOperationsThrow", "TryPeek and TryDequeue throw ObjectDisposedException after Dispose", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue.Create<int>();
                        queue.Enqueue(1);
                        queue.Dispose();

                        TestAssert.Throws<ObjectDisposedException>(() => queue.TryPeek(out _), "TryPeek disposed");
                        TestAssert.Throws<ObjectDisposedException>(() => queue.TryDequeue(out _), "TryDequeue disposed");
                    }),

                    TestCases.Sync(SuiteId, "DisposedExceptionNamesQueueType", "ObjectDisposedException identifies the queue type", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue.Create<int>();
                        queue.Dispose();

                        ObjectDisposedException thrown = TestAssert.Throws<ObjectDisposedException>(() => queue.Enqueue(1));
                        TestAssert.True(thrown.ObjectName.Contains("SelectorQueue"), "Object name '" + thrown.ObjectName + "'");
                    }),

                    TestCases.Sync(SuiteId, "DisposeAfterDrainIsSafe", "Dispose after the queue has been drained disposes nothing", () =>
                    {
                        SelectorQueue<DisposableTracker> queue = SelectorQueue.Create<DisposableTracker>();
                        DisposableTracker tracker = new DisposableTracker("a");
                        queue.Enqueue(tracker);
                        queue.Dequeue();
                        queue.Dispose();

                        TestAssert.Equal(0, tracker.DisposeCount, "Dispose count");
                    }),

                    TestCases.Sync(SuiteId, "ClearPropagatesItemDisposeException", "Exception thrown by an item's Dispose propagates from Clear, and the queue is still emptied", () =>
                    {
                        SelectorQueue<DisposableTracker> queue = SelectorQueue.Create<DisposableTracker>();
                        DisposableTracker throwing = new DisposableTracker("throwing", throwOnDispose: true);
                        queue.Enqueue(throwing);

                        TestAssert.Throws<InvalidOperationException>(() => queue.Clear(), "Clear with throwing item");
                        TestAssert.Equal(1, throwing.DisposeCount, "Throwing item dispose attempted");
                        TestAssert.Equal(0, queue.Count, "Queue emptied");

                        queue.Enqueue(new DisposableTracker("next"));
                        TestAssert.Equal(1, queue.Count, "Queue remains usable");
                    }),

                    TestCases.Sync(SuiteId, "DisposePropagatesItemDisposeException", "Exception thrown by an item's Dispose propagates from Dispose, and the queue is still closed", () =>
                    {
                        SelectorQueue<DisposableTracker> queue = SelectorQueue.Create<DisposableTracker>();
                        queue.Enqueue(new DisposableTracker("throwing", throwOnDispose: true));

                        TestAssert.Throws<InvalidOperationException>(() => queue.Dispose(), "Dispose with throwing item");
                        TestAssert.Throws<ObjectDisposedException>(() => queue.Enqueue(new DisposableTracker("late")), "Queue closed");
                        queue.Dispose();
                    })
                });
        }
    }
}
