namespace Test.Automated
{
    using Test.Automated.Suites;

    /// <summary>
    /// Entry point for the automated SelectorQueue test runner.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Runs the automated test suites.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the test runner.</param>
        /// <returns><c>0</c> when all tests pass; otherwise <c>1</c>.</returns>
        public static async Task<int> Main(string[] args)
        {
            if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
            {
                Console.WriteLine("Usage: Test.Automated [--help]");
                Console.WriteLine();
                Console.WriteLine("Runs the SelectorQueue automated test suite.");
                return 0;
            }

            TestRunner runner = new TestRunner("SELECTORQUEUE AUTOMATED TEST SUITE");

            runner.AddSuite(new BasicOperationTests());
            runner.AddSuite(new OrderingTests());
            runner.AddSuite(new SelectorConfigurationTests());
            runner.AddSuite(new ThreadSafetyTests());
            runner.AddSuite(new RandomizedCorrectnessTests());
            runner.AddSuite(new StressTests());
            runner.AddSuite(new PerformanceTests());
            runner.AddSuite(new EdgeCaseTests());
            runner.AddSuite(new RealWorldScenarioTests());

            return await runner.RunAllAsync().ConfigureAwait(false);
        }
    }
}
