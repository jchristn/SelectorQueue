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
    /// Verifies ordering correctness, stability, and null-key behavior.
    /// </summary>
    public static class OrderingSuite
    {
        /// <summary>
        /// The suite identifier.
        /// </summary>
        public const string SuiteId = "Ordering";

        /// <summary>
        /// Creates the suite descriptor.
        /// </summary>
        /// <returns>The suite descriptor.</returns>
        public static TestSuiteDescriptor Create()
        {
            return new TestSuiteDescriptor(
                suiteId: SuiteId,
                displayName: "Ordering",
                cases: new List<TestCaseDescriptor>
                {
                    TestCases.Sync(SuiteId, "StableOrderingIsPreservedForEqualPrimaryKeys", "Stable ordering is preserved for equal primary keys", () =>
                    {
                        SelectorQueue<TaggedItem> queue = SelectorQueue
                            .OrderBy<TaggedItem, int>(x => x.Priority)
                            .Build();

                        queue.Enqueue(new TaggedItem(5, "first"));
                        queue.Enqueue(new TaggedItem(5, "second"));
                        queue.Enqueue(new TaggedItem(5, "third"));

                        TestAssert.Equal("first", queue.Dequeue().Tag);
                        TestAssert.Equal("second", queue.Dequeue().Tag);
                        TestAssert.Equal("third", queue.Dequeue().Tag);
                    }),

                    TestCases.Sync(SuiteId, "StableOrderingIsPreservedWhenPrimaryAndSecondaryKeysAreEqual", "Stable ordering is preserved when primary and secondary keys are equal", () =>
                    {
                        SelectorQueue<OrderingWorkItem> queue = SelectorQueue
                            .OrderBy<OrderingWorkItem, int>(x => x.Priority)
                            .ThenBy<string?>(x => x.Group)
                            .Build();

                        queue.Enqueue(new OrderingWorkItem("a", 1, "ops"));
                        queue.Enqueue(new OrderingWorkItem("b", 1, "ops"));
                        queue.Enqueue(new OrderingWorkItem("c", 1, "ops"));

                        TestAssert.Equal("a", queue.Dequeue().Id);
                        TestAssert.Equal("b", queue.Dequeue().Id);
                        TestAssert.Equal("c", queue.Dequeue().Id);
                    }),

                    TestCases.Sync(SuiteId, "CompositeOrderingSupportsAscendingAndDescendingSelectors", "Composite ordering supports ascending and descending selectors", () =>
                    {
                        SelectorQueue<OrderingWorkItem> queue = SelectorQueue
                            .OrderBy<OrderingWorkItem, int>(x => x.Priority)
                            .ThenByDescending<int>(x => x.Score)
                            .ThenBy<string>(x => x.Id)
                            .Build();

                        queue.Enqueue(new OrderingWorkItem("c", 1, "ops", 10));
                        queue.Enqueue(new OrderingWorkItem("a", 1, "ops", 20));
                        queue.Enqueue(new OrderingWorkItem("b", 1, "ops", 20));
                        queue.Enqueue(new OrderingWorkItem("d", 2, "ops", 100));

                        TestAssert.Equal("a", queue.Dequeue().Id);
                        TestAssert.Equal("b", queue.Dequeue().Id);
                        TestAssert.Equal("c", queue.Dequeue().Id);
                        TestAssert.Equal("d", queue.Dequeue().Id);
                    }),

                    TestCases.Sync(SuiteId, "NullKeysSortBeforeNonNullKeysWithDefaultComparerSemantics", "Null keys sort before non-null keys with default comparer semantics", () =>
                    {
                        SelectorQueue<OrderingWorkItem> queue = SelectorQueue
                            .OrderBy<OrderingWorkItem, string?>(x => x.Group)
                            .ThenBy<string>(x => x.Id)
                            .Build();

                        queue.Enqueue(new OrderingWorkItem("b", 0, "ops"));
                        queue.Enqueue(new OrderingWorkItem("a", 0, null));
                        queue.Enqueue(new OrderingWorkItem("c", 0, null));

                        TestAssert.Equal("a", queue.Dequeue().Id);
                        TestAssert.Equal("c", queue.Dequeue().Id);
                        TestAssert.Equal("b", queue.Dequeue().Id);
                    }),

                    TestCases.Sync(SuiteId, "HeavyNondeterministicEnqueueOrderDrainsInSortedOrder", "Heavy nondeterministic enqueue order drains in sorted order", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderBy<int, int>(x => x)
                            .Build();

                        Random random = new Random(4567);
                        List<int> values = new List<int>(5000);
                        for (int i = 0; i < 5000; i++)
                        {
                            int value = random.Next(0, 100000);
                            values.Add(value);
                            queue.Enqueue(value);
                        }

                        values.Sort();
                        for (int i = 0; i < values.Count; i++)
                        {
                            TestAssert.Equal(values[i], queue.Dequeue(), "Sorted drain index " + i);
                        }
                    }),

                    TestCases.Sync(SuiteId, "DescendingStringKeysPlaceNullsLast", "Descending reference keys place nulls last", () =>
                    {
                        SelectorQueue<OrderingWorkItem> queue = SelectorQueue
                            .OrderByDescending<OrderingWorkItem, string?>(x => x.Group)
                            .Build();

                        queue.Enqueue(new OrderingWorkItem("n", 0, null));
                        queue.Enqueue(new OrderingWorkItem("a", 0, "alpha"));
                        queue.Enqueue(new OrderingWorkItem("z", 0, "zulu"));

                        TestAssert.Equal("z", queue.Dequeue().Id, "First");
                        TestAssert.Equal("a", queue.Dequeue().Id, "Second");
                        TestAssert.Equal("n", queue.Dequeue().Id, "Null last");
                    }),

                    TestCases.Sync(SuiteId, "NullableValueKeysSortNullFirst", "Nullable value-type keys sort null before values in ascending order", () =>
                    {
                        SelectorQueue<int?> queue = SelectorQueue
                            .OrderBy<int?, int?>(x => x)
                            .Build();

                        queue.Enqueue(5);
                        queue.Enqueue(null);
                        queue.Enqueue(-3);

                        TestAssert.Null(queue.Dequeue(), "Null first");
                        TestAssert.Equal((int?)(-3), queue.Dequeue(), "Negative second");
                        TestAssert.Equal((int?)5, queue.Dequeue(), "Positive third");
                    }),

                    TestCases.Sync(SuiteId, "EnumKeysOrderByUnderlyingValue", "Enum keys order by their underlying value", () =>
                    {
                        SelectorQueue<WorkSeverity> queue = SelectorQueue
                            .OrderByDescending<WorkSeverity, WorkSeverity>(x => x)
                            .Build();

                        queue.Enqueue(WorkSeverity.Medium);
                        queue.Enqueue(WorkSeverity.Critical);
                        queue.Enqueue(WorkSeverity.Low);
                        queue.Enqueue(WorkSeverity.High);

                        TestAssert.Equal(WorkSeverity.Critical, queue.Dequeue());
                        TestAssert.Equal(WorkSeverity.High, queue.Dequeue());
                        TestAssert.Equal(WorkSeverity.Medium, queue.Dequeue());
                        TestAssert.Equal(WorkSeverity.Low, queue.Dequeue());
                    }),

                    TestCases.Sync(SuiteId, "CustomComparableKeys", "Custom IComparable<T> keys use their CompareTo implementation", () =>
                    {
                        SelectorQueue<VersionKey> queue = SelectorQueue
                            .OrderBy<VersionKey, VersionKey>(x => x)
                            .Build();

                        queue.Enqueue(new VersionKey(2, 0));
                        queue.Enqueue(new VersionKey(1, 10));
                        queue.Enqueue(new VersionKey(1, 2));

                        VersionKey first = queue.Dequeue();
                        VersionKey second = queue.Dequeue();
                        VersionKey third = queue.Dequeue();
                        TestAssert.True(first.Major == 1 && first.Minor == 2, "First 1.2");
                        TestAssert.True(second.Major == 1 && second.Minor == 10, "Second 1.10");
                        TestAssert.True(third.Major == 2 && third.Minor == 0, "Third 2.0");
                    }),

                    TestCases.Sync(SuiteId, "ExtremeIntegerKeys", "Integer extremes order correctly without overflow", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderBy<int, int>(x => x)
                            .Build();

                        queue.Enqueue(0);
                        queue.Enqueue(int.MaxValue);
                        queue.Enqueue(int.MinValue);
                        queue.Enqueue(-1);

                        TestAssert.Equal(int.MinValue, queue.Dequeue());
                        TestAssert.Equal(-1, queue.Dequeue());
                        TestAssert.Equal(0, queue.Dequeue());
                        TestAssert.Equal(int.MaxValue, queue.Dequeue());
                    }),

                    TestCases.Sync(SuiteId, "DescendingExtremeIntegerKeys", "Descending integer extremes order correctly without overflow", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderByDescending<int, int>(x => x)
                            .Build();

                        queue.Enqueue(int.MinValue);
                        queue.Enqueue(0);
                        queue.Enqueue(int.MaxValue);

                        TestAssert.Equal(int.MaxValue, queue.Dequeue());
                        TestAssert.Equal(0, queue.Dequeue());
                        TestAssert.Equal(int.MinValue, queue.Dequeue());
                    }),

                    TestCases.Sync(SuiteId, "DoubleKeysWithNaNAndInfinity", "Double keys follow default comparer semantics for NaN and infinities", () =>
                    {
                        SelectorQueue<double> queue = SelectorQueue
                            .OrderBy<double, double>(x => x)
                            .Build();

                        queue.Enqueue(1.5);
                        queue.Enqueue(double.PositiveInfinity);
                        queue.Enqueue(double.NaN);
                        queue.Enqueue(double.NegativeInfinity);

                        TestAssert.True(double.IsNaN(queue.Dequeue()), "NaN first");
                        TestAssert.Equal(double.NegativeInfinity, queue.Dequeue(), "Negative infinity");
                        TestAssert.Equal(1.5, queue.Dequeue(), "Finite value");
                        TestAssert.Equal(double.PositiveInfinity, queue.Dequeue(), "Positive infinity");
                    }),

                    TestCases.Sync(SuiteId, "DescendingPrimaryKeepsStableTies", "Descending ordering still dequeues equal keys in enqueue order", () =>
                    {
                        SelectorQueue<TaggedItem> queue = SelectorQueue
                            .OrderByDescending<TaggedItem, int>(x => x.Priority)
                            .Build();

                        queue.Enqueue(new TaggedItem(1, "low"));
                        queue.Enqueue(new TaggedItem(5, "high-1"));
                        queue.Enqueue(new TaggedItem(5, "high-2"));
                        queue.Enqueue(new TaggedItem(5, "high-3"));

                        TestAssert.Equal("high-1", queue.Dequeue().Tag);
                        TestAssert.Equal("high-2", queue.Dequeue().Tag);
                        TestAssert.Equal("high-3", queue.Dequeue().Tag);
                        TestAssert.Equal("low", queue.Dequeue().Tag);
                    }),

                    TestCases.Sync(SuiteId, "StabilityAcrossInterleavedDequeues", "Equal keys stay in enqueue order across interleaved dequeues", () =>
                    {
                        SelectorQueue<TaggedItem> queue = SelectorQueue
                            .OrderBy<TaggedItem, int>(x => x.Priority)
                            .Build();

                        for (int i = 0; i < 5; i++) queue.Enqueue(new TaggedItem(1, "t" + i));
                        TestAssert.Equal("t0", queue.Dequeue().Tag);
                        TestAssert.Equal("t1", queue.Dequeue().Tag);

                        for (int i = 5; i < 8; i++) queue.Enqueue(new TaggedItem(1, "t" + i));
                        queue.Enqueue(new TaggedItem(0, "urgent"));

                        List<string> drained = new List<string>();
                        while (queue.TryDequeue(out TaggedItem item)) drained.Add(item.Tag);

                        TestAssert.SequenceEqual(new List<string> { "urgent", "t2", "t3", "t4", "t5", "t6", "t7" }, drained, "Drain order");
                    }),

                    TestCases.Sync(SuiteId, "LargeEqualKeyStability", "Ten thousand equal keys drain in exact enqueue order", () =>
                    {
                        SelectorQueue<TaggedItem> queue = SelectorQueue
                            .OrderBy<TaggedItem, int>(x => x.Priority)
                            .Build();

                        for (int i = 0; i < 10000; i++) queue.Enqueue(new TaggedItem(7, i.ToString()));
                        for (int i = 0; i < 10000; i++)
                        {
                            TestAssert.Equal(i.ToString(), queue.Dequeue().Tag, "Index " + i);
                        }
                    }),

                    TestCases.Sync(SuiteId, "FourLevelCompositeOrdering", "Four-level composite ordering resolves each tie in turn", () =>
                    {
                        SelectorQueue<RandomizedRecord> queue = SelectorQueue
                            .OrderBy<RandomizedRecord, int>(x => x.Primary)
                            .ThenByDescending<int>(x => x.Secondary)
                            .ThenBy<string?>(x => x.Group)
                            .ThenByDescending<int>(x => x.Id)
                            .Build();

                        queue.Enqueue(new RandomizedRecord(1, 1, 5, "b"));
                        queue.Enqueue(new RandomizedRecord(2, 1, 5, "a"));
                        queue.Enqueue(new RandomizedRecord(3, 1, 5, "a"));
                        queue.Enqueue(new RandomizedRecord(4, 1, 9, "z"));
                        queue.Enqueue(new RandomizedRecord(5, 0, 0, null));

                        List<int> drained = new List<int>();
                        while (queue.TryDequeue(out RandomizedRecord record)) drained.Add(record.Id);

                        TestAssert.SequenceEqual(new List<int> { 5, 4, 3, 2, 1 }, drained, "Composite order");
                    }),

                    TestCases.Sync(SuiteId, "PeekMatchesNextDequeue", "Peek always matches the next Dequeue through a full random drain", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderByDescending<int, int>(x => x % 100)
                            .ThenBy<int>(x => x)
                            .Build();

                        Random random = new Random(777);
                        for (int i = 0; i < 2000; i++) queue.Enqueue(random.Next(0, 100000));

                        while (queue.Count > 0)
                        {
                            int peeked = queue.Peek();
                            TestAssert.Equal(peeked, queue.Dequeue(), "Peek vs Dequeue");
                        }
                    })
                });
        }

    }
}
