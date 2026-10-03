namespace Test.Shared.Models
{
    using System;

    /// <summary>
    /// Helper model used by real-world scheduling tests.
    /// </summary>
    public sealed class RealWorldJob
    {
        /// <summary>
        /// Initializes a helper model used by real-world scheduling tests.
        /// </summary>
        /// <param name="id">The job identifier.</param>
        /// <param name="priority">The business priority.</param>
        /// <param name="createdUtc">The creation time in UTC.</param>
        public RealWorldJob(string id, int priority, DateTime createdUtc)
        {
            Id = id;
            Priority = priority;
            CreatedUtc = createdUtc;
        }

        /// <summary>
        /// Gets the job identifier.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the business priority.
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Gets the creation time in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; }
    }
}
