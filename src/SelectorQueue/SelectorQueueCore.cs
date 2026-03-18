namespace SelectorQueue
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A thread-safe ordered queue with immutable ordering and stable dequeue behavior for equal sort keys.
    /// The queue owns items while they remain enqueued and will dispose queued values that implement
    /// <see cref="IDisposable"/> when <see cref="Clear"/> or <see cref="Dispose"/> is called.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public sealed class SelectorQueue<T> : IDisposable
    {
        private readonly object _Sync = new object();
        private readonly IOrderingCriterion<T>[] _Criteria;
        private readonly List<HeapEntry<T>> _Heap = new List<HeapEntry<T>>();
        private long _SequenceCounter;
        private bool _Disposed;

        /// <summary>
        /// Initializes a queue with the supplied immutable ordering criteria.
        /// </summary>
        /// <param name="criteria">The compiled ordering criteria used for all queue operations.</param>
        internal SelectorQueue(IOrderingCriterion<T>[] criteria)
        {
            _Criteria = criteria ?? Array.Empty<IOrderingCriterion<T>>();
        }

        /// <summary>
        /// Gets the current number of items in the queue.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_Sync)
                {
                    ThrowIfDisposed();
                    return _Heap.Count;
                }
            }
        }

        /// <summary>
        /// Enqueues an item. Selector delegates are evaluated before the item is inserted, so the queue remains unchanged if selector evaluation fails.
        /// Ownership of the item transfers to the queue until it is dequeued or the queue is cleared or disposed.
        /// </summary>
        /// <param name="item">The item to enqueue.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the queue has already been disposed.</exception>
        public void Enqueue(T item)
        {
            object?[]? keys = CreateKeys(item);

            lock (_Sync)
            {
                ThrowIfDisposed();
                HeapEntry<T> entry = new HeapEntry<T>(item, keys, _SequenceCounter++);
                Insert(entry);
            }
        }

        /// <summary>
        /// Removes and returns the next item according to the queue ordering.
        /// Ownership of the returned item transfers back to the caller.
        /// </summary>
        /// <returns>The next queued item.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the queue has already been disposed.</exception>
        public T Dequeue()
        {
            lock (_Sync)
            {
                ThrowIfDisposed();
                if (_Heap.Count == 0) throw new InvalidOperationException("The queue is empty.");

                HeapEntry<T> root = _Heap[0];
                RemoveRoot();
                return root.Value;
            }
        }

        /// <summary>
        /// Attempts to remove and return the next item according to the queue ordering.
        /// Ownership of the returned item transfers back to the caller when the method succeeds.
        /// </summary>
        /// <param name="item">When this method returns, contains the dequeued item if present.</param>
        /// <returns><c>true</c> when an item was dequeued; otherwise <c>false</c>.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the queue has already been disposed.</exception>
        public bool TryDequeue(out T item)
        {
            lock (_Sync)
            {
                ThrowIfDisposed();
                if (_Heap.Count == 0)
                {
                    item = default!;
                    return false;
                }

                HeapEntry<T> root = _Heap[0];
                RemoveRoot();
                item = root.Value;
                return true;
            }
        }

        /// <summary>
        /// Returns the next item according to the queue ordering without removing it.
        /// </summary>
        /// <returns>The next queued item.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the queue has already been disposed.</exception>
        public T Peek()
        {
            lock (_Sync)
            {
                ThrowIfDisposed();
                if (_Heap.Count == 0) throw new InvalidOperationException("The queue is empty.");
                return _Heap[0].Value;
            }
        }

        /// <summary>
        /// Attempts to return the next item according to the queue ordering without removing it.
        /// </summary>
        /// <param name="item">When this method returns, contains the peeked item if present.</param>
        /// <returns><c>true</c> when an item was present; otherwise <c>false</c>.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the queue has already been disposed.</exception>
        public bool TryPeek(out T item)
        {
            lock (_Sync)
            {
                ThrowIfDisposed();
                if (_Heap.Count == 0)
                {
                    item = default!;
                    return false;
                }

                item = _Heap[0].Value;
                return true;
            }
        }

        /// <summary>
        /// Removes all queued items and disposes any queued values that implement <see cref="IDisposable"/>.
        /// The queue retains its immutable ordering configuration.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the queue has already been disposed.</exception>
        public void Clear()
        {
            List<HeapEntry<T>> entriesToDispose;

            lock (_Sync)
            {
                ThrowIfDisposed();
                entriesToDispose = new List<HeapEntry<T>>(_Heap);
                _Heap.Clear();
            }

            DisposeEntries(entriesToDispose);
        }

        /// <summary>
        /// Disposes the queue and any queued values that implement <see cref="IDisposable"/>.
        /// Items already dequeued are not affected.
        /// </summary>
        public void Dispose()
        {
            List<HeapEntry<T>> entriesToDispose;

            lock (_Sync)
            {
                if (_Disposed) return;

                _Disposed = true;
                entriesToDispose = new List<HeapEntry<T>>(_Heap);
                _Heap.Clear();
            }

            DisposeEntries(entriesToDispose);
        }

        private object?[]? CreateKeys(T item)
        {
            if (_Criteria.Length == 0) return null;

            object?[] keys = new object?[_Criteria.Length];
            for (int i = 0; i < _Criteria.Length; i++)
            {
                keys[i] = _Criteria[i].Select(item);
            }

            return keys;
        }

        private void RemoveRoot()
        {
            int lastIndex = _Heap.Count - 1;
            if (lastIndex == 0)
            {
                _Heap.RemoveAt(0);
                return;
            }

            _Heap[0] = _Heap[lastIndex];
            _Heap.RemoveAt(lastIndex);
            SiftDown(0);
        }

        private void Insert(HeapEntry<T> entry)
        {
            if (_Heap.Count == 0)
            {
                _Heap.Add(entry);
                return;
            }

            List<int>? ancestorsToShift = null;
            int insertionIndex = _Heap.Count;

            while (insertionIndex > 0)
            {
                int parentIndex = (insertionIndex - 1) / 2;
                if (CompareEntries(entry, _Heap[parentIndex]) >= 0) break;

                if (ancestorsToShift == null)
                {
                    ancestorsToShift = new List<int>();
                }

                ancestorsToShift.Add(parentIndex);
                insertionIndex = parentIndex;
            }

            _Heap.Add(entry);

            if (ancestorsToShift == null) return;

            int holeIndex = _Heap.Count - 1;
            for (int i = 0; i < ancestorsToShift.Count; i++)
            {
                int parentIndex = ancestorsToShift[i];
                _Heap[holeIndex] = _Heap[parentIndex];
                holeIndex = parentIndex;
            }

            _Heap[holeIndex] = entry;
        }

        private void SiftDown(int index)
        {
            while (true)
            {
                int left = (index * 2) + 1;
                if (left >= _Heap.Count) return;

                int right = left + 1;
                int smallest = left;

                if (right < _Heap.Count && CompareEntries(_Heap[right], _Heap[left]) < 0)
                {
                    smallest = right;
                }

                if (CompareEntries(_Heap[smallest], _Heap[index]) >= 0) return;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int left, int right)
        {
            HeapEntry<T> temp = _Heap[left];
            _Heap[left] = _Heap[right];
            _Heap[right] = temp;
        }

        private int CompareEntries(HeapEntry<T> left, HeapEntry<T> right)
        {
            for (int i = 0; i < _Criteria.Length; i++)
            {
                int result = _Criteria[i].Compare(left.Keys![i], right.Keys![i]);
                if (result != 0) return result;
            }

            return left.Sequence.CompareTo(right.Sequence);
        }

        private static void DisposeEntries(List<HeapEntry<T>> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                DisposeValue(entries[i].Value);
            }
        }

        private static void DisposeValue(T value)
        {
            if (value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_Disposed)
            {
                throw new ObjectDisposedException(typeof(SelectorQueue<T>).FullName);
            }
        }
    }
}
