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
    /// Verifies queue behavior against representative real-world usage scenarios.
    /// </summary>
    public static class RealWorldScenarioSuite
    {
        /// <summary>
        /// The suite identifier.
        /// </summary>
        public const string SuiteId = "RealWorldScenario";

        /// <summary>
        /// Creates the suite descriptor.
        /// </summary>
        /// <returns>The suite descriptor.</returns>
        public static TestSuiteDescriptor Create()
        {
            return new TestSuiteDescriptor(
                suiteId: SuiteId,
                displayName: "Real World Scenarios",
                cases: new List<TestCaseDescriptor>
                {
                    TestCases.Sync(SuiteId, "S3StyleMetadataArrivesNondeterministicallyAndDrainsInProcessingOrder", "S3-style metadata arrives nondeterministically and drains in processing order", () =>
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

                        TestAssert.Equal("call_001.wav", queue.Dequeue().Key);
                        TestAssert.Equal("call_002.wav", queue.Dequeue().Key);
                        TestAssert.Equal("call_003.wav", queue.Dequeue().Key);
                        TestAssert.Equal("call_004.wav", queue.Dequeue().Key);
                    }),

                    TestCases.Sync(SuiteId, "SchedulerStyleQueueMixesBusinessPriorityAndAge", "Scheduler-style queue mixes business priority and age", () =>
                    {
                        SelectorQueue<RealWorldJob> queue = SelectorQueue
                            .OrderByDescending<RealWorldJob, int>(x => x.Priority)
                            .ThenBy<DateTime>(x => x.CreatedUtc)
                            .Build();

                        queue.Enqueue(new RealWorldJob("standard-old", 1, DateTime.Parse("2025-06-15T12:00:00Z")));
                        queue.Enqueue(new RealWorldJob("urgent-new", 3, DateTime.Parse("2025-06-15T14:00:00Z")));
                        queue.Enqueue(new RealWorldJob("urgent-old", 3, DateTime.Parse("2025-06-15T11:00:00Z")));
                        queue.Enqueue(new RealWorldJob("normal", 2, DateTime.Parse("2025-06-15T13:00:00Z")));

                        TestAssert.Equal("urgent-old", queue.Dequeue().Id);
                        TestAssert.Equal("urgent-new", queue.Dequeue().Id);
                        TestAssert.Equal("normal", queue.Dequeue().Id);
                        TestAssert.Equal("standard-old", queue.Dequeue().Id);
                    }),

                });
        }

    }
}
