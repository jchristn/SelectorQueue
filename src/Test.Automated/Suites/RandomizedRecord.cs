namespace Test.Automated.Suites
{
    /// <summary>
    /// Helper model used by randomized correctness tests.
    /// </summary>
    internal sealed class RandomizedRecord
    {
        /// <summary>
        /// Initializes a helper model used by randomized correctness tests.
        /// </summary>
        /// <param name="id">The stable insertion identifier.</param>
        /// <param name="primary">The primary sort key.</param>
        /// <param name="secondary">The secondary sort key.</param>
        /// <param name="group">The tertiary grouping key.</param>
        public RandomizedRecord(int id, int primary, int secondary, string? group)
        {
            Id = id;
            Primary = primary;
            Secondary = secondary;
            Group = group;
        }

        /// <summary>
        /// Gets the stable insertion identifier.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the primary sort key.
        /// </summary>
        public int Primary { get; }

        /// <summary>
        /// Gets the secondary sort key.
        /// </summary>
        public int Secondary { get; }

        /// <summary>
        /// Gets the tertiary grouping key.
        /// </summary>
        public string? Group { get; }
    }
}
