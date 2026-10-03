namespace Test.Shared
{
    using System.Collections.Generic;
    using Test.Shared.Suites;
    using Touchstone.Core;

    /// <summary>
    /// Central source of truth for every SelectorQueue test suite. All runners consume <see cref="All"/>.
    /// </summary>
    public static class SelectorQueueSuites
    {
        /// <summary>
        /// Gets every test suite in execution order.
        /// </summary>
        public static IReadOnlyList<TestSuiteDescriptor> All
        {
            get
            {
                return new List<TestSuiteDescriptor>
                {
                    BasicOperationSuite.Create(),
                    OrderingSuite.Create(),
                    SelectorConfigurationSuite.Create(),
                    SelectorBehaviorSuite.Create(),
                    OwnershipSuite.Create(),
                    ExceptionSafetySuite.Create(),
                    ThreadSafetySuite.Create(),
                    ConcurrentLifecycleSuite.Create(),
                    RandomizedCorrectnessSuite.Create(),
                    StressSuite.Create(),
                    PerformanceSuite.Create(),
                    EdgeCaseSuite.Create(),
                    RealWorldScenarioSuite.Create()
                };
            }
        }
    }
}
