namespace Test.Shared.Models
{
    /// <summary>
    /// Helper class for testing insertion order stability.
    /// </summary>
    public class TaggedItem
    {
        /// <summary>
        /// Gets or sets the logical ordering priority.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Gets or sets a tag used to identify the item in assertions.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Initializes a tagged test item.
        /// </summary>
        /// <param name="priority">The item's priority.</param>
        /// <param name="tag">The identifier tag used in assertions.</param>
        public TaggedItem(int priority, string tag)
        {
            Priority = priority;
            Tag = tag;
        }
    }
}
