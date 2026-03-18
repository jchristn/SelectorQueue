namespace SelectorQueue
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Builds a <see cref="SelectorQueue{T}"/> with immutable ordering.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public sealed class SelectorQueueBuilder<T>
    {
        private readonly List<IOrderingCriterion<T>> _Criteria = new List<IOrderingCriterion<T>>();

        /// <summary>
        /// Configures the primary ascending ordering selector and discards any previously configured ordering.
        /// </summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <param name="selector">The selector used to extract the primary sort key.</param>
        /// <returns>The current builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="selector"/> is null.</exception>
        public SelectorQueueBuilder<T> OrderBy<TKey>(Func<T, TKey> selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            _Criteria.Clear();
            _Criteria.Add(new OrderingCriterion<T, TKey>(selector, descending: false));
            return this;
        }

        /// <summary>
        /// Configures the primary descending ordering selector and discards any previously configured ordering.
        /// </summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <param name="selector">The selector used to extract the primary sort key.</param>
        /// <returns>The current builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="selector"/> is null.</exception>
        public SelectorQueueBuilder<T> OrderByDescending<TKey>(Func<T, TKey> selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            _Criteria.Clear();
            _Criteria.Add(new OrderingCriterion<T, TKey>(selector, descending: true));
            return this;
        }

        /// <summary>
        /// Appends an ascending secondary ordering selector.
        /// </summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <param name="selector">The selector used to extract the secondary sort key.</param>
        /// <returns>The current builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="selector"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no primary ordering has been configured.</exception>
        public SelectorQueueBuilder<T> ThenBy<TKey>(Func<T, TKey> selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            if (_Criteria.Count == 0) throw new InvalidOperationException("ThenBy must follow OrderBy or OrderByDescending.");

            _Criteria.Add(new OrderingCriterion<T, TKey>(selector, descending: false));
            return this;
        }

        /// <summary>
        /// Appends a descending secondary ordering selector.
        /// </summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <param name="selector">The selector used to extract the secondary sort key.</param>
        /// <returns>The current builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="selector"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no primary ordering has been configured.</exception>
        public SelectorQueueBuilder<T> ThenByDescending<TKey>(Func<T, TKey> selector)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            if (_Criteria.Count == 0) throw new InvalidOperationException("ThenByDescending must follow OrderBy or OrderByDescending.");

            _Criteria.Add(new OrderingCriterion<T, TKey>(selector, descending: true));
            return this;
        }

        /// <summary>
        /// Builds a queue whose ordering configuration is frozen for the lifetime of the queue.
        /// </summary>
        /// <returns>A new queue instance.</returns>
        public SelectorQueue<T> Build()
        {
            return new SelectorQueue<T>(_Criteria.ToArray());
        }
    }
}
