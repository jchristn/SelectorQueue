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
    /// Verifies edge-case behavior including nulls, selector failures, and large-volume smoke coverage.
    /// </summary>
    public static class EdgeCaseSuite
    {
        /// <summary>
        /// The suite identifier.
        /// </summary>
        public const string SuiteId = "EdgeCase";

        /// <summary>
        /// Creates the suite descriptor.
        /// </summary>
        /// <returns>The suite descriptor.</returns>
        public static TestSuiteDescriptor Create()
        {
            return new TestSuiteDescriptor(
                suiteId: SuiteId,
                displayName: "Edge Cases",
                cases: new List<TestCaseDescriptor>
                {
                    TestCases.Sync(SuiteId, "NullValuesAreSupportedForNullableReferenceTypes", "Null values are supported for nullable reference types", () =>
                    {
                        SelectorQueue<string?> queue = SelectorQueue.Create<string?>();
                        queue.Enqueue(null);
                        queue.Enqueue("hello");

                        TestAssert.True(queue.Dequeue() == null, "First value should be null");
                        TestAssert.Equal("hello", queue.Dequeue());
                    }),

                    TestCases.Sync(SuiteId, "SelectorExceptionLeavesQueueUnchanged", "Selector exception leaves queue unchanged", () =>
                    {
                        SelectorQueue<EdgeCaseWorkItem> queue = SelectorQueue
                            .OrderBy<EdgeCaseWorkItem, int>(x =>
                            {
                                if (x.Priority < 0) throw new InvalidOperationException("bad key");
                                return x.Priority;
                            })
                            .Build();

                        queue.Enqueue(new EdgeCaseWorkItem("ok", 1));
                        TestAssert.Throws<InvalidOperationException>(() => queue.Enqueue(new EdgeCaseWorkItem("bad", -1)));
                        TestAssert.Equal(1, queue.Count, "Count after failed enqueue");
                        TestAssert.Equal("ok", queue.Dequeue().Id);
                    }),

                    TestCases.Sync(SuiteId, "LargeVolumeSmokeTestMaintainsOrdering", "Large volume smoke test maintains ordering", () =>
                    {
                        SelectorQueue<CallRecording> queue = SelectorQueue
                            .OrderBy<CallRecording, DateTime>(x => x.Timestamp)
                            .ThenByDescending<long>(x => x.Size)
                            .ThenBy<string>(x => x.Key)
                            .Build();

                        DateTime baseTime = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);
                        for (int i = 99999; i >= 0; i--)
                        {
                            queue.Enqueue(new CallRecording("call-" + i.ToString("D5"), i % 2048, baseTime.AddSeconds(i % 1000)));
                        }

                        CallRecording previous = queue.Dequeue();
                        for (int i = 1; i < 100000; i++)
                        {
                            CallRecording current = queue.Dequeue();
                            TestAssert.LessThanOrEqual(previous.Timestamp, current.Timestamp, "Timestamp order");

                            if (previous.Timestamp == current.Timestamp)
                            {
                                TestAssert.GreaterThanOrEqual(previous.Size, current.Size, "Size tie-breaker");
                            }

                            previous = current;
                        }
                    }),

                    TestCases.Sync(SuiteId, "ClearOnAnEmptyQueueIsANoOp", "Clear on an empty queue is a no-op", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderBy<int, int>(x => x)
                            .Build();

                        queue.Clear();
                        TestAssert.Equal(0, queue.Count);
                        TestAssert.False(queue.TryDequeue(out _));
                    }),

                    TestCases.Sync(SuiteId, "ClearDisposesQueuedDisposableItems", "Clear disposes queued disposable items", () =>
                    {
                        SelectorQueue<DisposableTracker> queue = SelectorQueue
                            .OrderBy<DisposableTracker, string>(x => x.Name)
                            .Build();

                        DisposableTracker first = new DisposableTracker("a");
                        DisposableTracker second = new DisposableTracker("b");

                        queue.Enqueue(first);
                        queue.Enqueue(second);
                        queue.Clear();

                        TestAssert.Equal(1, first.DisposeCount, "First dispose count");
                        TestAssert.Equal(1, second.DisposeCount, "Second dispose count");
                        TestAssert.Equal(0, queue.Count, "Queue count");
                    }),

                    TestCases.Sync(SuiteId, "DequeuedDisposableItemIsNotDisposedByQueueDisposal", "Dequeued disposable item is not disposed by queue disposal", () =>
                    {
                        SelectorQueue<DisposableTracker> queue = SelectorQueue
                            .OrderBy<DisposableTracker, string>(x => x.Name)
                            .Build();

                        DisposableTracker dequeued = new DisposableTracker("a");
                        DisposableTracker remaining = new DisposableTracker("b");

                        queue.Enqueue(remaining);
                        queue.Enqueue(dequeued);

                        DisposableTracker result = queue.Dequeue();
                        queue.Dispose();

                        TestAssert.Equal("a", result.Name, "Dequeued item");
                        TestAssert.Equal(0, dequeued.DisposeCount, "Dequeued item dispose count");
                        TestAssert.Equal(1, remaining.DisposeCount, "Remaining item dispose count");
                    }),

                });
        }

    }
}
