namespace SelectorQueue
{
    /// <summary>
    /// Stores a queue item, its precomputed ordering keys, and its stable insertion sequence.
    /// </summary>
    /// <typeparam name="T">The queued value type.</typeparam>
    internal readonly struct HeapEntry<T>
    {
        /// <summary>
        /// Initializes a heap entry.
        /// </summary>
        /// <param name="value">The queued value.</param>
        /// <param name="keys">The precomputed ordering keys for the value.</param>
        /// <param name="sequence">The stable insertion sequence.</param>
        public HeapEntry(T value, object?[]? keys, long sequence)
        {
            Value = value;
            Keys = keys;
            Sequence = sequence;
        }

        /// <summary>
        /// Gets the queued value.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Gets the precomputed ordering keys.
        /// </summary>
        public object?[]? Keys { get; }

        /// <summary>
        /// Gets the stable insertion sequence.
        /// </summary>
        public long Sequence { get; }
    }
}
