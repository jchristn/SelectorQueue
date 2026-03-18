namespace Test.Automated.Suites
{
    /// <summary>
    /// Helper model used by ordering tests.
    /// </summary>
    internal sealed class OrderingWorkItem
    {
        /// <summary>
        /// Initializes a helper model used by ordering tests.
        /// </summary>
        /// <param name="id">The item identifier.</param>
        /// <param name="priority">The primary ordering priority.</param>
        /// <param name="group">The grouping key.</param>
        /// <param name="score">The secondary score.</param>
        public OrderingWorkItem(string id, int priority, string? group, int score = 0)
        {
            Id = id;
            Priority = priority;
            Group = group;
            Score = score;
        }

        /// <summary>
        /// Gets the item identifier.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the primary ordering priority.
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Gets the grouping key.
        /// </summary>
        public string? Group { get; }

        /// <summary>
        /// Gets the score used by compound ordering tests.
        /// </summary>
        public int Score { get; }
    }
}
