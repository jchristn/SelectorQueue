namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.XunitAdapter;
    using global::Xunit;

    /// <summary>
    /// Runs every shared descriptor sequentially in a single xUnit fact.
    /// </summary>
    public sealed class SelectorQueueFactTests : TouchstoneFactBase
    {
        /// <summary>
        /// Gets the shared suites to execute.
        /// </summary>
        protected override IReadOnlyList<TestSuiteDescriptor> Suites
        {
            get { return SelectorQueueSuites.All; }
        }

        /// <summary>
        /// Runs all shared suites.
        /// </summary>
        /// <returns>A task that completes when every suite has run.</returns>
        [Fact]
        public async Task RunAll()
        {
            await RunAllAsync();
        }
    }
}
