namespace Test.Automated
{
    using System;
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Cli;

    /// <summary>
    /// Entry point for the automated SelectorQueue console test runner.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Runs every shared test suite through the Touchstone console runner.
        /// </summary>
        /// <param name="args">Command-line arguments. Supports <c>--results &lt;path&gt;</c> and <c>--help</c>.</param>
        /// <returns><c>0</c> when all tests pass; otherwise <c>1</c>.</returns>
        public static async Task<int> Main(string[] args)
        {
            string? resultsPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--help" || args[i] == "-h")
                {
                    Console.WriteLine("Usage: Test.Automated [--results <path>] [--help]");
                    Console.WriteLine();
                    Console.WriteLine("Runs the SelectorQueue automated test suite.");
                    Console.WriteLine("  --results <path>  Write JSON test results to the specified file.");
                    return 0;
                }

                if (args[i] == "--results" && i + 1 < args.Length)
                {
                    resultsPath = args[i + 1];
                    i++;
                }
            }

            return await ConsoleRunner.RunAsync(
                SelectorQueueSuites.All,
                resultsPath: resultsPath).ConfigureAwait(false);
        }
    }
}
