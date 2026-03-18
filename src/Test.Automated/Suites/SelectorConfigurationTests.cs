namespace Test.Automated.Suites
{
    using SelectorQueue;

    /// <summary>
    /// Verifies builder configuration rules and immutable queue construction behavior.
    /// </summary>
    public class SelectorConfigurationTests : TestSuite
    {
        /// <summary>
        /// Gets the display name of this test suite.
        /// </summary>
        public override string Name => "Configuration";

        /// <summary>
        /// Executes the tests in this suite.
        /// </summary>
        /// <returns>A task that completes when all tests have run.</returns>
        protected override async Task RunTestsAsync()
        {
            await RunTest("ThenBy without a primary selector throws", () =>
            {
                SelectorQueueBuilder<int> builder = new SelectorQueueBuilder<int>();
                AssertThrows<InvalidOperationException>(() => builder.ThenBy(x => x));
            });

            await RunTest("ThenByDescending without a primary selector throws", () =>
            {
                SelectorQueueBuilder<int> builder = new SelectorQueueBuilder<int>();
                AssertThrows<InvalidOperationException>(() => builder.ThenByDescending(x => x));
            });

            await RunTest("Null selectors throw argument exceptions", () =>
            {
                SelectorQueueBuilder<int> builder = new SelectorQueueBuilder<int>();
                AssertThrows<ArgumentNullException>(() => builder.OrderBy<int>(null!));
                AssertThrows<ArgumentNullException>(() => builder.OrderByDescending<int>(null!));

                builder.OrderBy(x => x);
                AssertThrows<ArgumentNullException>(() => builder.ThenBy<int>(null!));
                AssertThrows<ArgumentNullException>(() => builder.ThenByDescending<int>(null!));
            });

            await RunTest("Multiple OrderBy calls reset builder ordering", () =>
            {
                SelectorQueue<int> queue = new SelectorQueueBuilder<int>()
                    .OrderBy(x => x)
                    .ThenByDescending(x => x)
                    .OrderByDescending(x => x)
                    .Build();

                queue.Enqueue(1);
                queue.Enqueue(3);
                queue.Enqueue(2);

                AssertEqual(3, queue.Dequeue());
                AssertEqual(2, queue.Dequeue());
                AssertEqual(1, queue.Dequeue());
            });

            await RunTest("Built queue is unaffected by later builder changes", () =>
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

                AssertEqual(1, ascending.Dequeue(), "Ascending queue");
                AssertEqual(3, descending.Dequeue(), "Descending queue");
            });

            await RunTest("Static factory overloads build ordered queues", () =>
            {
                SelectorQueue<int> queue = SelectorQueue
                    .OrderByDescending<int, int>(x => x)
                    .Build();

                queue.Enqueue(2);
                queue.Enqueue(3);
                queue.Enqueue(1);

                AssertEqual(3, queue.Dequeue());
                AssertEqual(2, queue.Dequeue());
                AssertEqual(1, queue.Dequeue());
            });
        }
    }
}
