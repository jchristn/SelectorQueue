namespace Test.Shared.Suites
{
    using System;
    using System.Collections.Generic;
    using SelectorQueue;
    using Test.Shared.Models;
    using Touchstone.Core;

    /// <summary>
    /// Verifies when selectors run, how keys are captured, and how selector and comparison failures are surfaced.
    /// </summary>
    public static class SelectorBehaviorSuite
    {
        /// <summary>
        /// The suite identifier.
        /// </summary>
        public const string SuiteId = "SelectorBehavior";

        /// <summary>
        /// Creates the suite descriptor.
        /// </summary>
        /// <returns>The suite descriptor.</returns>
        public static TestSuiteDescriptor Create()
        {
            return new TestSuiteDescriptor(
                suiteId: SuiteId,
                displayName: "Selector Behavior",
                cases: new List<TestCaseDescriptor>
                {
                    TestCases.Sync(SuiteId, "SelectorsRunOncePerCriterionPerEnqueue", "Each selector runs exactly once per enqueue", () =>
                    {
                        int primaryCalls = 0;
                        int secondaryCalls = 0;
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderBy<int, int>(x => { primaryCalls++; return x; })
                            .ThenByDescending<int>(x => { secondaryCalls++; return -x; })
                            .Build();

                        for (int i = 0; i < 50; i++)
                        {
                            queue.Enqueue(50 - i);
                        }

                        TestAssert.Equal(50, primaryCalls, "Primary selector calls");
                        TestAssert.Equal(50, secondaryCalls, "Secondary selector calls");
                    }),

                    TestCases.Sync(SuiteId, "SelectorsNotInvokedByReadOperations", "Peek, Dequeue, Count, and Clear never invoke selectors", () =>
                    {
                        int calls = 0;
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderBy<int, int>(x => { calls++; return x; })
                            .Build();

                        queue.Enqueue(2);
                        queue.Enqueue(1);
                        queue.Enqueue(3);
                        int afterEnqueue = calls;

                        queue.Peek();
                        queue.TryPeek(out _);
                        int count = queue.Count;
                        queue.Dequeue();
                        queue.TryDequeue(out _);
                        queue.Clear();

                        TestAssert.Equal(3, afterEnqueue, "Calls after enqueue");
                        TestAssert.Equal(afterEnqueue, calls, "Calls after read operations");
                        TestAssert.Equal(3, count, "Count");
                    }),

                    TestCases.Sync(SuiteId, "FifoQueueNeverInvokesSelectors", "FIFO queue accepts items without any selector configured", () =>
                    {
                        SelectorQueue<MutableItem> queue = SelectorQueue.Create<MutableItem>();
                        queue.Enqueue(new MutableItem("a", 9));
                        queue.Enqueue(new MutableItem("b", 1));

                        TestAssert.Equal("a", queue.Dequeue().Id, "First FIFO item");
                        TestAssert.Equal("b", queue.Dequeue().Id, "Second FIFO item");
                    }),

                    TestCases.Sync(SuiteId, "KeysCapturedAtEnqueueTime", "Mutating an item after enqueue does not change its position", () =>
                    {
                        SelectorQueue<MutableItem> queue = SelectorQueue
                            .OrderBy<MutableItem, int>(x => x.Priority)
                            .Build();

                        MutableItem first = new MutableItem("first", 1);
                        MutableItem second = new MutableItem("second", 2);
                        queue.Enqueue(second);
                        queue.Enqueue(first);

                        first.Priority = 100;

                        TestAssert.Same(first, queue.Peek(), "Peek uses captured key");
                        TestAssert.Equal("first", queue.Dequeue().Id, "First dequeue");
                        TestAssert.Equal("second", queue.Dequeue().Id, "Second dequeue");
                    }),

                    TestCases.Sync(SuiteId, "SecondarySelectorExceptionLeavesQueueUnchanged", "Exception in a secondary selector propagates and leaves the queue unchanged", () =>
                    {
                        SelectorQueue<EdgeCaseWorkItem> queue = SelectorQueue
                            .OrderBy<EdgeCaseWorkItem, int>(x => x.Priority)
                            .ThenBy<string>(x =>
                            {
                                if (x.Id == "bad") throw new FormatException("bad secondary key");
                                return x.Id;
                            })
                            .Build();

                        queue.Enqueue(new EdgeCaseWorkItem("b", 1));
                        queue.Enqueue(new EdgeCaseWorkItem("a", 1));

                        FormatException thrown = TestAssert.Throws<FormatException>(() => queue.Enqueue(new EdgeCaseWorkItem("bad", 0)), "Secondary selector failure");
                        TestAssert.Equal("bad secondary key", thrown.Message, "Original exception message");
                        TestAssert.Equal(2, queue.Count, "Count after failed enqueue");
                        TestAssert.Equal("a", queue.Dequeue().Id, "First remaining");
                        TestAssert.Equal("b", queue.Dequeue().Id, "Second remaining");
                    }),

                    TestCases.Sync(SuiteId, "SelectorExceptionOnEmptyQueue", "Selector exception on an empty queue leaves it empty and usable", () =>
                    {
                        SelectorQueue<EdgeCaseWorkItem> queue = SelectorQueue
                            .OrderBy<EdgeCaseWorkItem, int>(x =>
                            {
                                if (x.Priority < 0) throw new ArgumentOutOfRangeException(nameof(x), "negative");
                                return x.Priority;
                            })
                            .Build();

                        TestAssert.Throws<ArgumentOutOfRangeException>(() => queue.Enqueue(new EdgeCaseWorkItem("bad", -1)), "Enqueue failure");
                        TestAssert.Equal(0, queue.Count, "Count after failure");
                        TestAssert.False(queue.TryPeek(out _), "TryPeek after failure");

                        queue.Enqueue(new EdgeCaseWorkItem("ok", 5));
                        TestAssert.Equal("ok", queue.Dequeue().Id, "Queue usable after failure");
                    }),

                    TestCases.Sync(SuiteId, "SelectorExceptionDoesNotConsumeSequence", "Failed enqueue does not disturb stable ordering of later equal keys", () =>
                    {
                        SelectorQueue<TaggedItem> queue = SelectorQueue
                            .OrderBy<TaggedItem, int>(x =>
                            {
                                if (x.Tag == "bad") throw new InvalidOperationException("bad");
                                return x.Priority;
                            })
                            .Build();

                        queue.Enqueue(new TaggedItem(1, "a"));
                        TestAssert.Throws<InvalidOperationException>(() => queue.Enqueue(new TaggedItem(1, "bad")));
                        queue.Enqueue(new TaggedItem(1, "b"));
                        queue.Enqueue(new TaggedItem(1, "c"));

                        TestAssert.Equal("a", queue.Dequeue().Tag);
                        TestAssert.Equal("b", queue.Dequeue().Tag);
                        TestAssert.Equal("c", queue.Dequeue().Tag);
                    }),

                    TestCases.Sync(SuiteId, "NonComparableKeyFirstEnqueueSucceeds", "Non-comparable key type is accepted while no comparison is required", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderBy<int, NonComparableKey>(x => new NonComparableKey(x))
                            .Build();

                        queue.Enqueue(1);
                        TestAssert.Equal(1, queue.Count, "Count");
                        TestAssert.Equal(1, queue.Dequeue(), "Single item");
                    }),

                    TestCases.Sync(SuiteId, "NonComparableKeyComparisonThrows", "Non-comparable key type throws on comparison and leaves the queue unchanged", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderBy<int, NonComparableKey>(x => new NonComparableKey(x))
                            .Build();

                        queue.Enqueue(1);
                        TestAssert.Throws<ArgumentException>(() => queue.Enqueue(2), "Second enqueue");
                        TestAssert.Equal(1, queue.Count, "Count after failed comparison");
                        TestAssert.Equal(1, queue.Dequeue(), "Original item retained");
                    }),

                    TestCases.Sync(SuiteId, "NullSelectorResultForReferenceKey", "Selector returning null for every item degrades to FIFO", () =>
                    {
                        SelectorQueue<TaggedItem> queue = SelectorQueue
                            .OrderBy<TaggedItem, string?>(x => null)
                            .Build();

                        queue.Enqueue(new TaggedItem(3, "first"));
                        queue.Enqueue(new TaggedItem(1, "second"));
                        queue.Enqueue(new TaggedItem(2, "third"));

                        TestAssert.Equal("first", queue.Dequeue().Tag);
                        TestAssert.Equal("second", queue.Dequeue().Tag);
                        TestAssert.Equal("third", queue.Dequeue().Tag);
                    }),

                    TestCases.Sync(SuiteId, "SelectorReceivesNullItem", "Selector receives null items and may produce keys for them", () =>
                    {
                        SelectorQueue<string?> queue = SelectorQueue
                            .OrderBy<string?, int>(x => x == null ? int.MaxValue : x.Length)
                            .Build();

                        queue.Enqueue(null);
                        queue.Enqueue("abc");
                        queue.Enqueue("a");

                        TestAssert.Equal("a", queue.Dequeue(), "Shortest");
                        TestAssert.Equal("abc", queue.Dequeue(), "Longer");
                        TestAssert.Null(queue.Dequeue(), "Null item last");
                    }),

                    TestCases.Sync(SuiteId, "SelectorNullReferenceOnNullItem", "Selector that dereferences a null item throws and leaves the queue unchanged", () =>
                    {
                        SelectorQueue<TaggedItem?> queue = SelectorQueue
                            .OrderBy<TaggedItem?, int>(x => x!.Priority)
                            .Build();

                        queue.Enqueue(new TaggedItem(1, "ok"));
                        TestAssert.Throws<NullReferenceException>(() => queue.Enqueue(null), "Null item");
                        TestAssert.Equal(1, queue.Count, "Count");
                    })
                });
        }
    }
}
