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
    /// Verifies builder configuration rules and immutable queue construction behavior.
    /// </summary>
    public static class SelectorConfigurationSuite
    {
        /// <summary>
        /// The suite identifier.
        /// </summary>
        public const string SuiteId = "SelectorConfiguration";

        /// <summary>
        /// Creates the suite descriptor.
        /// </summary>
        /// <returns>The suite descriptor.</returns>
        public static TestSuiteDescriptor Create()
        {
            return new TestSuiteDescriptor(
                suiteId: SuiteId,
                displayName: "Configuration",
                cases: new List<TestCaseDescriptor>
                {
                    TestCases.Sync(SuiteId, "ThenByWithoutAPrimarySelectorThrows", "ThenBy without a primary selector throws", () =>
                    {
                        SelectorQueueBuilder<int> builder = new SelectorQueueBuilder<int>();
                        TestAssert.Throws<InvalidOperationException>(() => builder.ThenBy(x => x));
                    }),

                    TestCases.Sync(SuiteId, "ThenByDescendingWithoutAPrimarySelectorThrows", "ThenByDescending without a primary selector throws", () =>
                    {
                        SelectorQueueBuilder<int> builder = new SelectorQueueBuilder<int>();
                        TestAssert.Throws<InvalidOperationException>(() => builder.ThenByDescending(x => x));
                    }),

                    TestCases.Sync(SuiteId, "NullSelectorsThrowArgumentExceptions", "Null selectors throw argument exceptions", () =>
                    {
                        SelectorQueueBuilder<int> builder = new SelectorQueueBuilder<int>();
                        TestAssert.Throws<ArgumentNullException>(() => builder.OrderBy<int>(null!));
                        TestAssert.Throws<ArgumentNullException>(() => builder.OrderByDescending<int>(null!));

                        builder.OrderBy(x => x);
                        TestAssert.Throws<ArgumentNullException>(() => builder.ThenBy<int>(null!));
                        TestAssert.Throws<ArgumentNullException>(() => builder.ThenByDescending<int>(null!));
                    }),

                    TestCases.Sync(SuiteId, "MultipleOrderByCallsResetBuilderOrdering", "Multiple OrderBy calls reset builder ordering", () =>
                    {
                        SelectorQueue<int> queue = new SelectorQueueBuilder<int>()
                            .OrderBy(x => x)
                            .ThenByDescending(x => x)
                            .OrderByDescending(x => x)
                            .Build();

                        queue.Enqueue(1);
                        queue.Enqueue(3);
                        queue.Enqueue(2);

                        TestAssert.Equal(3, queue.Dequeue());
                        TestAssert.Equal(2, queue.Dequeue());
                        TestAssert.Equal(1, queue.Dequeue());
                    }),

                    TestCases.Sync(SuiteId, "BuiltQueueIsUnaffectedByLaterBuilderChanges", "Built queue is unaffected by later builder changes", () =>
                    {
                        SelectorQueueBuilder<int> builder = new SelectorQueueBuilder<int>()
                            .OrderBy(x => x);

                        SelectorQueue<int> ascending = builder.Build();
                        builder.OrderByDescending(x => x);
                        SelectorQueue<int> descending = builder.Build();

                        ascending.Enqueue(3);
                        ascending.Enqueue(1);

                        descending.Enqueue(3);
                        descending.Enqueue(1);

                        TestAssert.Equal(1, ascending.Dequeue(), "Ascending queue");
                        TestAssert.Equal(3, descending.Dequeue(), "Descending queue");
                    }),

                    TestCases.Sync(SuiteId, "StaticFactoryOverloadsBuildOrderedQueues", "Static factory overloads build ordered queues", () =>
                    {
                        SelectorQueue<int> queue = SelectorQueue
                            .OrderByDescending<int, int>(x => x)
                            .Build();

                        queue.Enqueue(2);
                        queue.Enqueue(3);
                        queue.Enqueue(1);

                        TestAssert.Equal(3, queue.Dequeue());
                        TestAssert.Equal(2, queue.Dequeue());
                        TestAssert.Equal(1, queue.Dequeue());
                    }),

                    TestCases.Sync(SuiteId, "StaticFactoryNullSelectorsThrow", "Static factory methods reject null selectors", () =>
                    {
                        TestAssert.Throws<ArgumentNullException>(() => SelectorQueue.OrderBy<int, int>(null!), "OrderBy");
                        TestAssert.Throws<ArgumentNullException>(() => SelectorQueue.OrderByDescending<int, int>(null!), "OrderByDescending");
                    }),

                    TestCases.Sync(SuiteId, "NullSelectorParameterName", "ArgumentNullException names the selector parameter", () =>
                    {
                        ArgumentNullException thrown = TestAssert.Throws<ArgumentNullException>(() => new SelectorQueueBuilder<int>().OrderBy<int>(null!));
                        TestAssert.Equal("selector", thrown.ParamName, "Parameter name");
                    }),

                    TestCases.Sync(SuiteId, "ThenByWithoutPrimaryMessage", "ThenBy without a primary selector explains the required call order", () =>
                    {
                        InvalidOperationException thrown = TestAssert.Throws<InvalidOperationException>(() => new SelectorQueueBuilder<int>().ThenBy(x => x));
                        TestAssert.True(thrown.Message.Contains("OrderBy"), "Message '" + thrown.Message + "'");
                    }),

                    TestCases.Sync(SuiteId, "BuilderMethodsAreFluent", "Builder methods return the same builder instance", () =>
                    {
                        SelectorQueueBuilder<int> builder = new SelectorQueueBuilder<int>();
                        TestAssert.Same(builder, builder.OrderBy(x => x), "OrderBy");
                        TestAssert.Same(builder, builder.ThenBy(x => x), "ThenBy");
                        TestAssert.Same(builder, builder.ThenByDescending(x => x), "ThenByDescending");
                        TestAssert.Same(builder, builder.OrderByDescending(x => x), "OrderByDescending");
                    }),

                    TestCases.Sync(SuiteId, "FailedConfigurationLeavesBuilderUsable", "Rejected builder calls do not change existing configuration", () =>
                    {
                        SelectorQueueBuilder<int> builder = new SelectorQueueBuilder<int>();
                        TestAssert.Throws<InvalidOperationException>(() => builder.ThenBy(x => x), "ThenBy before OrderBy");
                        builder.OrderByDescending(x => x);
                        TestAssert.Throws<ArgumentNullException>(() => builder.ThenBy<int>(null!), "Null ThenBy");

                        SelectorQueue<int> queue = builder.Build();
                        queue.Enqueue(1);
                        queue.Enqueue(3);
                        queue.Enqueue(2);

                        TestAssert.Equal(3, queue.Dequeue(), "Descending first");
                        TestAssert.Equal(2, queue.Dequeue(), "Descending second");
                    }),

                    TestCases.Sync(SuiteId, "EmptyBuilderBuildsFifoQueue", "Building without any ordering yields a FIFO queue", () =>
                    {
                        SelectorQueue<int> queue = new SelectorQueueBuilder<int>().Build();
                        queue.Enqueue(3);
                        queue.Enqueue(1);
                        queue.Enqueue(2);

                        TestAssert.Equal(3, queue.Dequeue());
                        TestAssert.Equal(1, queue.Dequeue());
                        TestAssert.Equal(2, queue.Dequeue());
                    }),

                    TestCases.Sync(SuiteId, "BuildTwiceProducesIndependentQueues", "Multiple Build calls produce independent queues", () =>
                    {
                        SelectorQueueBuilder<int> builder = SelectorQueue.OrderBy<int, int>(x => x);
                        SelectorQueue<int> first = builder.Build();
                        SelectorQueue<int> second = builder.Build();

                        first.Enqueue(1);
                        TestAssert.Equal(1, first.Count, "First count");
                        TestAssert.Equal(0, second.Count, "Second count");

                        first.Dispose();
                        second.Enqueue(2);
                        TestAssert.Equal(2, second.Dequeue(), "Second unaffected by first dispose");
                    }),

                    TestCases.Sync(SuiteId, "ThenByAfterBuildDoesNotAffectQueue", "Appending ThenBy after Build does not change the built queue", () =>
                    {
                        SelectorQueueBuilder<TaggedItem> builder = SelectorQueue.OrderBy<TaggedItem, int>(x => x.Priority);
                        SelectorQueue<TaggedItem> queue = builder.Build();
                        builder.ThenByDescending(x => x.Tag);

                        queue.Enqueue(new TaggedItem(1, "a"));
                        queue.Enqueue(new TaggedItem(1, "b"));

                        TestAssert.Equal("a", queue.Dequeue().Tag, "Stable order retained");
                        TestAssert.Equal("b", queue.Dequeue().Tag, "Second");
                    }),

                    TestCases.Sync(SuiteId, "MixedKeyTypesAcrossCriteria", "Each criterion may use a different key type", () =>
                    {
                        SelectorQueue<CallRecording> queue = new SelectorQueueBuilder<CallRecording>()
                            .OrderBy(x => x.Timestamp.Date)
                            .ThenByDescending(x => x.Size)
                            .ThenBy(x => x.Key)
                            .Build();

                        DateTime day = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);
                        queue.Enqueue(new CallRecording("b", 10, day.AddHours(5)));
                        queue.Enqueue(new CallRecording("a", 10, day.AddHours(1)));
                        queue.Enqueue(new CallRecording("c", 99, day.AddHours(9)));
                        queue.Enqueue(new CallRecording("d", 1, day.AddDays(-1)));

                        TestAssert.Equal("d", queue.Dequeue().Key, "Previous day");
                        TestAssert.Equal("c", queue.Dequeue().Key, "Largest size");
                        TestAssert.Equal("a", queue.Dequeue().Key, "Key tie-break a");
                        TestAssert.Equal("b", queue.Dequeue().Key, "Key tie-break b");
                    })
                });
        }

    }
}
