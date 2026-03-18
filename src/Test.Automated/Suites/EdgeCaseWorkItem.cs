namespace Test.Automated.Suites
{
    /// <summary>
    /// Helper model used by edge case tests.
    /// </summary>
    internal sealed class EdgeCaseWorkItem
    {
        /// <summary>
        /// Initializes a helper model used by edge case tests.
        /// </summary>
        /// <param name="id">The item identifier.</param>
        /// <param name="priority">The test priority.</param>
        public EdgeCaseWorkItem(string id, int priority)
        {
            Id = id;
            Priority = priority;
        }

        /// <summary>
        /// Gets the item identifier.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the test priority.
        /// </summary>
        public int Priority { get; }
    }
}
