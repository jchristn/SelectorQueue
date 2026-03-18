namespace SelectorQueue
{
    /// <summary>
    /// Represents one compiled ordering criterion for queued items.
    /// </summary>
    /// <typeparam name="T">The queue item type.</typeparam>
    internal interface IOrderingCriterion<T>
    {
        /// <summary>
        /// Extracts the comparison key for the supplied item.
        /// </summary>
        /// <param name="item">The item being enqueued.</param>
        /// <returns>The extracted key value.</returns>
        object? Select(T item);

        /// <summary>
        /// Compares two previously extracted key values.
        /// </summary>
        /// <param name="left">The left key value.</param>
        /// <param name="right">The right key value.</param>
        /// <returns>
        /// A value less than zero when <paramref name="left"/> sorts before <paramref name="right"/>,
        /// zero when they are equivalent, or a value greater than zero otherwise.
        /// </returns>
        int Compare(object? left, object? right);
    }
}
