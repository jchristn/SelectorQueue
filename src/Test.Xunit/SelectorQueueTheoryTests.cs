namespace Test.Xunit
{
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Core;
    using global::Xunit;
    using global::Xunit.Abstractions;

    /// <summary>
    /// Runs each shared descriptor as a separate xUnit theory row.
    /// </summary>
    public sealed class SelectorQueueTheoryTests
    {
        private readonly ITestOutputHelper _Output;

        /// <summary>
        /// Initializes the theory test class.
        /// </summary>
        /// <param name="output">The xUnit output helper.</param>
        public SelectorQueueTheoryTests(ITestOutputHelper output)
        {
            _Output = output;
        }

        /// <summary>
        /// Gets every non-skipped shared test case.
        /// </summary>
        /// <returns>The theory data.</returns>
        public static TheoryData<TestCaseDescriptor> TestCases()
        {
            TheoryData<TestCaseDescriptor> data = new TheoryData<TestCaseDescriptor>();

            foreach (TestSuiteDescriptor suite in SelectorQueueSuites.All)
            {
                foreach (TestCaseDescriptor testCase in suite.Cases)
                {
                    if (!testCase.Skip)
                        data.Add(testCase);
                }
            }

            return data;
        }

        /// <summary>
        /// Runs one shared test case.
        /// </summary>
        /// <param name="testCase">The test case to run.</param>
        /// <returns>A task that completes when the test case has run.</returns>
        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task RunTest(TestCaseDescriptor testCase)
        {
            _Output.WriteLine("Running: " + testCase.DisplayName);
            await testCase.ExecuteAsync(CancellationToken.None);
        }
    }
}
