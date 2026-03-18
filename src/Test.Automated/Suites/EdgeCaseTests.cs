namespace Test.Automated.Suites
{
    using SelectorQueue;

    /// <summary>
    /// Verifies edge-case behavior including nulls, selector failures, and large-volume smoke coverage.
    /// </summary>
    public class EdgeCaseTests : TestSuite
    {
        /// <summary>
        /// Gets the display name of this test suite.
        /// </summary>
        public override string Name => "Edge Cases";

        /// <summary>
        /// Executes the tests in this suite.
        /// </summary>
        /// <returns>A task that completes when all tests have run.</returns>
        protected override async Task RunTestsAsync()
        {
            await RunTest("Null values are supported for nullable reference types", () =>
            {
                SelectorQueue<string?> queue = SelectorQueue.Create<string?>();
                queue.Enqueue(null);
                queue.Enqueue("hello");

                Assert(queue.Dequeue() == null, "First value should be null");
                AssertEqual("hello", queue.Dequeue());
            });

            await RunTest("Selector exception leaves queue unchanged", () =>
            {
                SelectorQueue<EdgeCaseWorkItem> queue = SelectorQueue
                    .OrderBy<EdgeCaseWorkItem, int>(x =>
                    {
                        if (x.Priority < 0) throw new InvalidOperationException("bad key");
                        return x.Priority;
                    })
                    .Build();

                queue.Enqueue(new EdgeCaseWorkItem("ok", 1));
                AssertThrows<InvalidOperationException>(() => queue.Enqueue(new EdgeCaseWorkItem("bad", -1)));
                AssertEqual(1, queue.Count, "Count after failed enqueue");
                AssertEqual("ok", queue.Dequeue().Id);
            });

            await RunTest("Large volume smoke test maintains ordering", () =>
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
                    AssertLessThanOrEqual(previous.Timestamp, current.Timestamp, "Timestamp order");

                    if (previous.Timestamp == current.Timestamp)
                    {
                        AssertGreaterThanOrEqual(previous.Size, current.Size, "Size tie-breaker");
                    }

                    previous = current;
                }
            });

            await RunTest("Clear on an empty queue is a no-op", () =>
            {
                SelectorQueue<int> queue = SelectorQueue
                    .OrderBy<int, int>(x => x)
                    .Build();

                queue.Clear();
                AssertEqual(0, queue.Count);
                AssertFalse(queue.TryDequeue(out _));
            });

            await RunTest("Clear disposes queued disposable items", () =>
            {
                SelectorQueue<DisposableTracker> queue = SelectorQueue
                    .OrderBy<DisposableTracker, string>(x => x.Name)
                    .Build();

                DisposableTracker first = new DisposableTracker("a");
                DisposableTracker second = new DisposableTracker("b");

                queue.Enqueue(first);
                queue.Enqueue(second);
                queue.Clear();

                AssertEqual(1, first.DisposeCount, "First dispose count");
                AssertEqual(1, second.DisposeCount, "Second dispose count");
                AssertEqual(0, queue.Count, "Queue count");
            });

            await RunTest("Dequeued disposable item is not disposed by queue disposal", () =>
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

                AssertEqual("a", result.Name, "Dequeued item");
                AssertEqual(0, dequeued.DisposeCount, "Dequeued item dispose count");
                AssertEqual(1, remaining.DisposeCount, "Remaining item dispose count");
            });
        }

    }
}
