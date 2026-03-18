namespace SelectorQueue
{
    using System;

    /// <summary>
    /// Factory methods for creating <see cref="SelectorQueue{T}"/> instances with immutable ordering.
    /// </summary>
    public static class SelectorQueue
    {
        /// <summary>
        /// Creates a FIFO queue with no explicit ordering selectors.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <returns>A queue instance with immutable FIFO ordering.</returns>
        public static SelectorQueue<T> Create<T>()
        {
            return new SelectorQueueBuilder<T>().Build();
        }

        /// <summary>
        /// Creates a builder whose primary ordering is ascending by the supplied selector.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <param name="selector">The selector used to extract the primary sort key.</param>
        /// <returns>A builder that can be further configured and then built.</returns>
        public static SelectorQueueBuilder<T> OrderBy<T, TKey>(Func<T, TKey> selector)
        {
            return new SelectorQueueBuilder<T>().OrderBy(selector);
        }

        /// <summary>
        /// Creates a builder whose primary ordering is descending by the supplied selector.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <param name="selector">The selector used to extract the primary sort key.</param>
        /// <returns>A builder that can be further configured and then built.</returns>
        public static SelectorQueueBuilder<T> OrderByDescending<T, TKey>(Func<T, TKey> selector)
        {
            return new SelectorQueueBuilder<T>().OrderByDescending(selector);
        }
    }
}
