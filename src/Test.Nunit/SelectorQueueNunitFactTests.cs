namespace Test.Nunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    /// <summary>
    /// Runs every shared descriptor sequentially in a single NUnit test.
    /// </summary>
    [TestFixture]
    public sealed class SelectorQueueNunitFactTests : TouchstoneNunitBase
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
        [Test]
        public async Task RunAll()
        {
            await RunAllAsync().ConfigureAwait(false);
        }
    }
}
