namespace Test.Automated.Suites
{
    using SelectorQueue;

    /// <summary>
    /// Verifies queue behavior against representative real-world usage scenarios.
    /// </summary>
    public class RealWorldScenarioTests : TestSuite
    {
        /// <summary>
        /// Gets the display name of this test suite.
        /// </summary>
        public override string Name => "Real World Scenarios";

        /// <summary>
        /// Executes the tests in this suite.
        /// </summary>
        /// <returns>A task that completes when all tests have run.</returns>
        protected override async Task RunTestsAsync()
        {
            await RunTest("S3-style metadata arrives nondeterministically and drains in processing order", () =>
            {
                SelectorQueue<CallRecording> queue = SelectorQueue
                    .OrderBy<CallRecording, DateTime>(x => x.Timestamp)
                    .ThenByDescending<long>(x => x.Size)
                    .ThenBy<string>(x => x.Key)
                    .Build();

                queue.Enqueue(new CallRecording("call_003.wav", 50000, DateTime.Parse("2025-06-15T14:00:00Z")));
                queue.Enqueue(new CallRecording("call_004.wav", 50000, DateTime.Parse("2025-06-15T14:00:00Z")));
                queue.Enqueue(new CallRecording("call_001.wav", 30000, DateTime.Parse("2025-06-15T12:00:00Z")));
                queue.Enqueue(new CallRecording("call_002.wav", 40000, DateTime.Parse("2025-06-15T13:00:00Z")));

                AssertEqual("call_001.wav", queue.Dequeue().Key);
                AssertEqual("call_002.wav", queue.Dequeue().Key);
                AssertEqual("call_003.wav", queue.Dequeue().Key);
                AssertEqual("call_004.wav", queue.Dequeue().Key);
            });

            await RunTest("Scheduler-style queue mixes business priority and age", () =>
            {
                SelectorQueue<RealWorldJob> queue = SelectorQueue
                    .OrderByDescending<RealWorldJob, int>(x => x.Priority)
                    .ThenBy<DateTime>(x => x.CreatedUtc)
                    .Build();

                queue.Enqueue(new RealWorldJob("standard-old", 1, DateTime.Parse("2025-06-15T12:00:00Z")));
                queue.Enqueue(new RealWorldJob("urgent-new", 3, DateTime.Parse("2025-06-15T14:00:00Z")));
                queue.Enqueue(new RealWorldJob("urgent-old", 3, DateTime.Parse("2025-06-15T11:00:00Z")));
                queue.Enqueue(new RealWorldJob("normal", 2, DateTime.Parse("2025-06-15T13:00:00Z")));

                AssertEqual("urgent-old", queue.Dequeue().Id);
                AssertEqual("urgent-new", queue.Dequeue().Id);
                AssertEqual("normal", queue.Dequeue().Id);
                AssertEqual("standard-old", queue.Dequeue().Id);
            });
        }

    }
}
