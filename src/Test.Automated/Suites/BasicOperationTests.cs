namespace Test.Automated.Suites
{
    using SelectorQueue;

    /// <summary>
    /// Verifies the core queue operations and basic lifecycle behavior.
    /// </summary>
    public class BasicOperationTests : TestSuite
    {
        /// <summary>
        /// Gets the display name of this test suite.
        /// </summary>
        public override string Name => "Basic Operations";

        /// <summary>
        /// Executes the tests in this suite.
        /// </summary>
        /// <returns>A task that completes when all tests have run.</returns>
        protected override async Task RunTestsAsync()
        {
            await RunTest("Create without ordering behaves as FIFO", () =>
            {
                SelectorQueue<int> queue = SelectorQueue.Create<int>();
                queue.Enqueue(3);
                queue.Enqueue(1);
                queue.Enqueue(2);

                AssertEqual(3, queue.Dequeue());
                AssertEqual(1, queue.Dequeue());
                AssertEqual(2, queue.Dequeue());
                AssertEqual(0, queue.Count, "Count after drain");
            });

            await RunTest("Ordered enqueue and dequeue use configured sort order", () =>
            {
                SelectorQueue<int> queue = SelectorQueue
                    .OrderBy<int, int>(x => x)
                    .Build();

                queue.Enqueue(30);
                queue.Enqueue(10);
                queue.Enqueue(20);

                AssertEqual(10, queue.Peek(), "Peek");
                AssertEqual(10, queue.Dequeue(), "First");
                AssertEqual(20, queue.Dequeue(), "Second");
                AssertEqual(30, queue.Dequeue(), "Third");
            });

            await RunTest("TryPeek and TryDequeue report empty queue correctly", () =>
            {
                SelectorQueue<string> queue = SelectorQueue.Create<string>();

                AssertFalse(queue.TryPeek(out string peeked), "TryPeek empty");
                Assert(peeked == null, "Empty TryPeek default");
                AssertFalse(queue.TryDequeue(out string dequeued), "TryDequeue empty");
                Assert(dequeued == null, "Empty TryDequeue default");
            });

            await RunTest("Dequeue and Peek throw on empty queue", () =>
            {
                SelectorQueue<int> queue = SelectorQueue.Create<int>();
                AssertThrows<InvalidOperationException>(() => queue.Dequeue(), "Dequeue empty");
                AssertThrows<InvalidOperationException>(() => queue.Peek(), "Peek empty");
            });

            await RunTest("Clear removes items and preserves ordering for later use", () =>
            {
                SelectorQueue<int> queue = SelectorQueue
                    .OrderBy<int, int>(x => x)
                    .Build();

                queue.Enqueue(4);
                queue.Enqueue(1);
                queue.Clear();
                AssertEqual(0, queue.Count, "Count after clear");

                queue.Enqueue(9);
                queue.Enqueue(3);

                AssertEqual(3, queue.Dequeue());
                AssertEqual(9, queue.Dequeue());
            });

            await RunTest("Count remains accurate during mixed operations", () =>
            {
                SelectorQueue<int> queue = SelectorQueue
                    .OrderBy<int, int>(x => x)
                    .Build();

                for (int i = 0; i < 100; i++)
                {
                    queue.Enqueue(i);
                }

                AssertEqual(100, queue.Count, "Count after enqueue");

                for (int i = 0; i < 40; i++)
                {
                    queue.Dequeue();
                }

                AssertEqual(60, queue.Count, "Count after dequeue");

                queue.Clear();
                AssertEqual(0, queue.Count, "Count after clear");
            });

            await RunTest("Dispose releases queued disposable items", () =>
            {
                SelectorQueue<DisposableTracker> queue = SelectorQueue
                    .OrderBy<DisposableTracker, string>(x => x.Name)
                    .Build();

                DisposableTracker first = new DisposableTracker("b");
                DisposableTracker second = new DisposableTracker("a");

                queue.Enqueue(first);
                queue.Enqueue(second);
                queue.Dispose();

                AssertEqual(1, first.DisposeCount, "First dispose count");
                AssertEqual(1, second.DisposeCount, "Second dispose count");
            });

            await RunTest("Disposed queue throws ObjectDisposedException on operations", () =>
            {
                SelectorQueue<int> queue = SelectorQueue
                    .OrderBy<int, int>(x => x)
                    .Build();

                queue.Dispose();

                AssertThrows<ObjectDisposedException>(() => queue.Enqueue(1), "Enqueue disposed");
                AssertThrows<ObjectDisposedException>(() => queue.Dequeue(), "Dequeue disposed");
                AssertThrows<ObjectDisposedException>(() => queue.Peek(), "Peek disposed");
                AssertThrows<ObjectDisposedException>(() => queue.Clear(), "Clear disposed");
                AssertThrows<ObjectDisposedException>(() =>
                {
                    int count = queue.Count;
                }, "Count disposed");
            });
        }
    }
}
