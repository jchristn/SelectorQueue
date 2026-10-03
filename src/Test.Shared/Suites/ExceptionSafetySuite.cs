namespace Test.Shared.Suites
{
    using System;
    using System.Collections.Generic;
    using SelectorQueue;
    using Test.Shared.Models;
    using Touchstone.Core;

    /// <summary>
    /// Verifies that key comparison failures never corrupt the heap or lose items.
    /// </summary>
    public static class ExceptionSafetySuite
    {
        /// <summary>
        /// The suite identifier.
        /// </summary>
        public const string SuiteId = "ExceptionSafety";

        /// <summary>
        /// Creates the suite descriptor.
        /// </summary>
        /// <returns>The suite descriptor.</returns>
        public static TestSuiteDescriptor Create()
        {
            return new TestSuiteDescriptor(
                suiteId: SuiteId,
                displayName: "Exception Safety",
                cases: new List<TestCaseDescriptor>
                {
                    TestCases.Sync(SuiteId, "DequeueComparisonFailureKeepsItems", "Comparison failure during Dequeue propagates without losing any item", () =>
                    {
                        FaultSwitch fault = new FaultSwitch();
                        SelectorQueue<int> queue = CreateQueue(fault);
                        for (int i = 9; i >= 0; i--) queue.Enqueue(i);

                        fault.Armed = true;
                        TestAssert.Throws<InjectedFaultException>(() => queue.Dequeue(), "Armed Dequeue");
                        TestAssert.Equal(10, queue.Count, "Count after failed Dequeue");
                        TestAssert.Equal(0, queue.Peek(), "Head after failed Dequeue");

                        fault.Armed = false;
                        AssertDrainsInOrder(queue, 0, 10);
                    }),

                    TestCases.Sync(SuiteId, "TryDequeueComparisonFailureKeepsItems", "Comparison failure during TryDequeue propagates without losing any item", () =>
                    {
                        FaultSwitch fault = new FaultSwitch();
                        SelectorQueue<int> queue = CreateQueue(fault);
                        for (int i = 0; i < 25; i++) queue.Enqueue((i * 7) % 25);

                        fault.Armed = true;
                        TestAssert.Throws<InjectedFaultException>(() => queue.TryDequeue(out _), "Armed TryDequeue");
                        TestAssert.Equal(25, queue.Count, "Count after failed TryDequeue");

                        fault.Armed = false;
                        AssertDrainsInOrder(queue, 0, 25);
                    }),

                    TestCases.Sync(SuiteId, "RepeatedDequeueFailuresAreRecoverable", "Repeated Dequeue failures leave the queue fully recoverable", () =>
                    {
                        FaultSwitch fault = new FaultSwitch();
                        SelectorQueue<int> queue = CreateQueue(fault);
                        for (int i = 0; i < 100; i++) queue.Enqueue(99 - i);

                        fault.Armed = true;
                        for (int attempt = 0; attempt < 20; attempt++)
                        {
                            TestAssert.Throws<InjectedFaultException>(() => queue.Dequeue(), "Attempt " + attempt);
                        }

                        fault.Armed = false;
                        TestAssert.Equal(100, queue.Count, "Count after repeated failures");
                        AssertDrainsInOrder(queue, 0, 100);
                    }),

                    TestCases.Sync(SuiteId, "EnqueueComparisonFailureLeavesQueueUnchanged", "Comparison failure during Enqueue leaves the queue unchanged", () =>
                    {
                        FaultSwitch fault = new FaultSwitch();
                        SelectorQueue<int> queue = CreateQueue(fault);
                        for (int i = 0; i < 10; i++) queue.Enqueue(i + 10);

                        fault.Armed = true;
                        TestAssert.Throws<InjectedFaultException>(() => queue.Enqueue(0), "Armed Enqueue");
                        TestAssert.Equal(10, queue.Count, "Count after failed Enqueue");

                        fault.Armed = false;
                        AssertDrainsInOrder(queue, 10, 10);
                    }),

                    TestCases.Sync(SuiteId, "OperationsWithoutComparisonsSucceedWhileArmed", "Operations that need no comparison succeed even while comparisons fail", () =>
                    {
                        FaultSwitch fault = new FaultSwitch();
                        SelectorQueue<int> queue = CreateQueue(fault);
                        fault.Armed = true;

                        queue.Enqueue(5);
                        TestAssert.Equal(5, queue.Peek(), "Peek single item");
                        TestAssert.Equal(1, queue.Count, "Count single item");
                        TestAssert.Equal(5, queue.Dequeue(), "Dequeue single item");
                        TestAssert.Equal(0, fault.FaultCount, "No faults injected");
                    }),

                    TestCases.Sync(SuiteId, "RandomFaultInjectionMatchesModel", "Random comparison faults never diverge the queue from a reference model", () =>
                    {
                        const int seedCount = 10;
                        const int operationCount = 2000;

                        for (int seed = 0; seed < seedCount; seed++)
                        {
                            FaultSwitch fault = new FaultSwitch(20000 + seed, 0.05);
                            SelectorQueue<int> queue = CreateQueue(fault);
                            Random random = new Random(30000 + seed);
                            List<int> model = new List<int>();

                            for (int operation = 0; operation < operationCount; operation++)
                            {
                                string context = "seed " + seed + " op " + operation;
                                fault.Armed = true;

                                if (random.Next(0, 100) < 60)
                                {
                                    int value = random.Next(0, 500);
                                    try
                                    {
                                        queue.Enqueue(value);
                                        model.Add(value);
                                    }
                                    catch (InjectedFaultException)
                                    {
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        bool success = queue.TryDequeue(out int actual);
                                        model.Sort();
                                        TestAssert.Equal(model.Count > 0, success, "TryDequeue result " + context);
                                        if (success)
                                        {
                                            TestAssert.Equal(model[0], actual, "Dequeued value " + context);
                                            model.RemoveAt(0);
                                        }
                                    }
                                    catch (InjectedFaultException)
                                    {
                                    }
                                }

                                fault.Armed = false;
                                TestAssert.Equal(model.Count, queue.Count, "Count " + context);
                            }

                            TestAssert.GreaterThan(fault.FaultCount, 0, "Faults injected seed " + seed);

                            model.Sort();
                            for (int i = 0; i < model.Count; i++)
                            {
                                TestAssert.Equal(model[i], queue.Dequeue(), "Final drain seed " + seed + " index " + i);
                            }
                        }
                    })
                });
        }

        private static SelectorQueue<int> CreateQueue(FaultSwitch fault)
        {
            return SelectorQueue
                .OrderBy<int, FaultyKey>(x => new FaultyKey(x, fault))
                .Build();
        }

        private static void AssertDrainsInOrder(SelectorQueue<int> queue, int first, int count)
        {
            for (int i = 0; i < count; i++)
            {
                TestAssert.Equal(first + i, queue.Dequeue(), "Drain index " + i);
            }

            TestAssert.Equal(0, queue.Count, "Drained");
        }
    }
}
