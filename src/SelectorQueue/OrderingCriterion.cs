namespace SelectorQueue
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Stores one ordering selector plus its comparison direction.
    /// </summary>
    /// <typeparam name="T">The queue item type.</typeparam>
    /// <typeparam name="TKey">The extracted key type.</typeparam>
    internal sealed class OrderingCriterion<T, TKey> : IOrderingCriterion<T>
    {
        private readonly Func<T, TKey> _Selector;
        private readonly int _Direction;
        private readonly IComparer<TKey> _Comparer;

        /// <summary>
        /// Initializes a new ordering criterion.
        /// </summary>
        /// <param name="selector">The selector used to extract keys from queue items.</param>
        /// <param name="descending"><c>true</c> for descending order; otherwise <c>false</c>.</param>
        public OrderingCriterion(Func<T, TKey> selector, bool descending)
        {
            _Selector = selector;
            _Direction = descending ? -1 : 1;
            _Comparer = Comparer<TKey>.Default;
        }

        /// <summary>
        /// Extracts the comparison key for the supplied item.
        /// </summary>
        /// <param name="item">The item being enqueued.</param>
        /// <returns>The extracted key value.</returns>
        public object? Select(T item)
        {
            return _Selector(item);
        }

        /// <summary>
        /// Compares two extracted key values using the configured direction.
        /// </summary>
        /// <param name="left">The left key value.</param>
        /// <param name="right">The right key value.</param>
        /// <returns>
        /// A value less than zero when <paramref name="left"/> sorts before <paramref name="right"/>,
        /// zero when they are equivalent, or a value greater than zero otherwise.
        /// </returns>
        public int Compare(object? left, object? right)
        {
            return _Direction * _Comparer.Compare((TKey?)left!, (TKey?)right!);
        }
    }
}
