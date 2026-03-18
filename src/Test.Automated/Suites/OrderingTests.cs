namespace Test.Automated.Suites
{
    using SelectorQueue;

    /// <summary>
    /// Verifies ordering correctness, stability, and null-key behavior.
    /// </summary>
    public class OrderingTests : TestSuite
    {
        /// <summary>
        /// Gets the display name of this test suite.
        /// </summary>
        public override string Name => "Ordering";

        /// <summary>
        /// Executes the tests in this suite.
        /// </summary>
        /// <returns>A task that completes when all tests have run.</returns>
        protected override async Task RunTestsAsync()
        {
            await RunTest("Stable ordering is preserved for equal primary keys", () =>
            {
                SelectorQueue<TaggedItem> queue = SelectorQueue
                    .OrderBy<TaggedItem, int>(x => x.Priority)
                    .Build();

                queue.Enqueue(new TaggedItem(5, "first"));
                queue.Enqueue(new TaggedItem(5, "second"));
                queue.Enqueue(new TaggedItem(5, "third"));

                AssertEqual("first", queue.Dequeue().Tag);
                AssertEqual("second", queue.Dequeue().Tag);
                AssertEqual("third", queue.Dequeue().Tag);
            });

            await RunTest("Stable ordering is preserved when primary and secondary keys are equal", () =>
            {
                SelectorQueue<OrderingWorkItem> queue = SelectorQueue
                    .OrderBy<OrderingWorkItem, int>(x => x.Priority)
                    .ThenBy<string?>(x => x.Group)
                    .Build();

                queue.Enqueue(new OrderingWorkItem("a", 1, "ops"));
                queue.Enqueue(new OrderingWorkItem("b", 1, "ops"));
                queue.Enqueue(new OrderingWorkItem("c", 1, "ops"));

                AssertEqual("a", queue.Dequeue().Id);
                AssertEqual("b", queue.Dequeue().Id);
                AssertEqual("c", queue.Dequeue().Id);
            });

            await RunTest("Composite ordering supports ascending and descending selectors", () =>
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

                AssertEqual("a", queue.Dequeue().Id);
                AssertEqual("b", queue.Dequeue().Id);
                AssertEqual("c", queue.Dequeue().Id);
                AssertEqual("d", queue.Dequeue().Id);
            });

            await RunTest("Null keys sort before non-null keys with default comparer semantics", () =>
            {
                SelectorQueue<OrderingWorkItem> queue = SelectorQueue
                    .OrderBy<OrderingWorkItem, string?>(x => x.Group)
                    .ThenBy<string>(x => x.Id)
                    .Build();

                queue.Enqueue(new OrderingWorkItem("b", 0, "ops"));
                queue.Enqueue(new OrderingWorkItem("a", 0, null));
                queue.Enqueue(new OrderingWorkItem("c", 0, null));

                AssertEqual("a", queue.Dequeue().Id);
                AssertEqual("c", queue.Dequeue().Id);
                AssertEqual("b", queue.Dequeue().Id);
            });

            await RunTest("Heavy nondeterministic enqueue order drains in sorted order", () =>
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
                    AssertEqual(values[i], queue.Dequeue(), "Sorted drain index " + i);
                }
            });
        }

    }
}
