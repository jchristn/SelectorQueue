namespace Test.Automated.Suites
{
    using SelectorQueue;

    /// <summary>
    /// Verifies queue behavior against randomized reference models.
    /// </summary>
    public class RandomizedCorrectnessTests : TestSuite
    {
        /// <summary>
        /// Gets the display name of this test suite.
        /// </summary>
        public override string Name => "Randomized Correctness";

        /// <summary>
        /// Executes the tests in this suite.
        /// </summary>
        /// <returns>A task that completes when all tests have run.</returns>
        protected override async Task RunTestsAsync()
        {
            await RunTest("Randomized compound ordering matches reference model across seeds", () =>
            {
                const int seedCount = 20;
                const int itemsPerSeed = 400;

                for (int seed = 0; seed < seedCount; seed++)
                {
                    Random random = new Random(8000 + seed);
                    SelectorQueue<RandomizedRecord> queue = SelectorQueue
                        .OrderBy<RandomizedRecord, int>(x => x.Primary)
                        .ThenByDescending<int>(x => x.Secondary)
                        .ThenBy<string?>(x => x.Group)
                        .Build();

                    List<RandomizedRecord> expected = new List<RandomizedRecord>(itemsPerSeed);
                    for (int i = 0; i < itemsPerSeed; i++)
                    {
                        RandomizedRecord record = new RandomizedRecord(
                            id: (seed * itemsPerSeed) + i,
                            primary: random.Next(0, 50),
                            secondary: random.Next(0, 100),
                            group: CreateRandomGroup(random));

                        expected.Add(record);
                        queue.Enqueue(record);
                    }

                    expected.Sort(CompareRandomizedRecords);
                    for (int i = 0; i < expected.Count; i++)
                    {
                        AssertEqual(expected[i].Id, queue.Dequeue().Id, "Seed " + seed + " index " + i);
                    }
                }
            });

            await RunTest("Randomized interleaved ordered operations match reference model", () =>
            {
                const int seedCount = 12;
                const int operationCount = 1200;

                for (int seed = 0; seed < seedCount; seed++)
                {
                    Random random = new Random(9000 + seed);
                    SelectorQueue<RandomizedRecord> queue = SelectorQueue
                        .OrderBy<RandomizedRecord, int>(x => x.Primary)
                        .ThenByDescending<int>(x => x.Secondary)
                        .ThenBy<string?>(x => x.Group)
                        .Build();

                    List<RandomizedRecord> model = new List<RandomizedRecord>();
                    int nextId = seed * 100000;

                    for (int operation = 0; operation < operationCount; operation++)
                    {
                        int action = random.Next(0, 100);
                        if (action < 60)
                        {
                            RandomizedRecord record = new RandomizedRecord(
                                id: nextId++,
                                primary: random.Next(0, 30),
                                secondary: random.Next(0, 100),
                                group: CreateRandomGroup(random));

                            queue.Enqueue(record);
                            model.Add(record);
                        }
                        else if (action < 80)
                        {
                            model.Sort(CompareRandomizedRecords);
                            bool expectedSuccess = model.Count > 0;

                            bool actualSuccess = queue.TryPeek(out RandomizedRecord? actual);
                            AssertEqual(expectedSuccess, actualSuccess, "TryPeek success seed " + seed + " op " + operation);

                            if (expectedSuccess)
                            {
                                AssertEqual(model[0].Id, actual.Id, "Peek id seed " + seed + " op " + operation);
                                AssertEqual(model[0].Id, queue.Peek().Id, "Peek sync seed " + seed + " op " + operation);
                            }
                        }
                        else
                        {
                            model.Sort(CompareRandomizedRecords);
                            bool expectedSuccess = model.Count > 0;

                            bool actualSuccess = queue.TryDequeue(out RandomizedRecord? actual);
                            AssertEqual(expectedSuccess, actualSuccess, "TryDequeue success seed " + seed + " op " + operation);

                            if (expectedSuccess)
                            {
                                AssertEqual(model[0].Id, actual.Id, "Dequeue id seed " + seed + " op " + operation);
                                model.RemoveAt(0);
                            }
                        }

                        AssertEqual(model.Count, queue.Count, "Count seed " + seed + " op " + operation);
                    }
                }
            });

            await RunTest("Randomized FIFO operations match queue reference", () =>
            {
                const int seedCount = 10;
                const int operationCount = 2000;

                for (int seed = 0; seed < seedCount; seed++)
                {
                    Random random = new Random(10000 + seed);
                    SelectorQueue<int> queue = SelectorQueue.Create<int>();
                    Queue<int> model = new Queue<int>();

                    for (int operation = 0; operation < operationCount; operation++)
                    {
                        int action = random.Next(0, 100);
                        if (action < 55)
                        {
                            int value = random.Next();
                            queue.Enqueue(value);
                            model.Enqueue(value);
                        }
                        else if (action < 75)
                        {
                            bool actualSuccess = queue.TryPeek(out int actual);
                            bool expectedSuccess = model.Count > 0;
                            AssertEqual(expectedSuccess, actualSuccess, "FIFO TryPeek success seed " + seed + " op " + operation);

                            if (expectedSuccess)
                            {
                                AssertEqual(model.Peek(), actual, "FIFO peek seed " + seed + " op " + operation);
                            }
                        }
                        else
                        {
                            bool actualSuccess = queue.TryDequeue(out int actual);
                            bool expectedSuccess = model.Count > 0;
                            AssertEqual(expectedSuccess, actualSuccess, "FIFO TryDequeue success seed " + seed + " op " + operation);

                            if (expectedSuccess)
                            {
                                AssertEqual(model.Dequeue(), actual, "FIFO dequeue seed " + seed + " op " + operation);
                            }
                        }

                        AssertEqual(model.Count, queue.Count, "FIFO count seed " + seed + " op " + operation);
                    }
                }
            });
        }

        private static int CompareRandomizedRecords(RandomizedRecord left, RandomizedRecord right)
        {
            int result = left.Primary.CompareTo(right.Primary);
            if (result != 0) return result;

            result = right.Secondary.CompareTo(left.Secondary);
            if (result != 0) return result;

            result = Comparer<string?>.Default.Compare(left.Group, right.Group);
            if (result != 0) return result;

            return left.Id.CompareTo(right.Id);
        }

        private static string? CreateRandomGroup(Random random)
        {
            int selector = random.Next(0, 6);
            if (selector == 0) return null;
            if (selector == 1) return "alpha";
            if (selector == 2) return "beta";
            if (selector == 3) return "gamma";
            if (selector == 4) return "delta";
            return "omega";
        }
    }
}
