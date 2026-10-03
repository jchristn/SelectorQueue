namespace Test.Shared.Models
{
    /// <summary>
    /// Helper model whose ordering key can be changed after enqueue.
    /// </summary>
    public sealed class MutableItem
    {
        /// <summary>
        /// Initializes a mutable helper model.
        /// </summary>
        /// <param name="id">The item identifier.</param>
        /// <param name="priority">The initial ordering priority.</param>
        public MutableItem(string id, int priority)
        {
            Id = id;
            Priority = priority;
        }

        /// <summary>
        /// Gets the item identifier.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets or sets the ordering priority.
        /// </summary>
        public int Priority { get; set; }
    }
}
