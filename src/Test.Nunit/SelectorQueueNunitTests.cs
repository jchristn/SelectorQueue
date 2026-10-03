namespace Test.Nunit
{
    using System.Collections;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    /// <summary>
    /// Runs each shared descriptor as a separate NUnit test case.
    /// </summary>
    [TestFixture]
    public sealed class SelectorQueueNunitTests
    {
        /// <summary>
        /// Runs one shared test case.
        /// </summary>
        /// <param name="testCase">The test case to run.</param>
        /// <returns>A task that completes when the test case has run.</returns>
        [Test]
        [TestCaseSource(nameof(TestCases))]
        public async Task RunTest(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        private static IEnumerable TestCases()
        {
            return new TouchstoneTestCaseSource(SelectorQueueSuites.All);
        }
    }
}
