namespace Test.Shared.Models
{
    using System;
    using System.Threading;

    /// <summary>
    /// Tracks disposal events for tests that verify queue ownership semantics.
    /// Disposal counting is thread-safe.
    /// </summary>
    public sealed class DisposableTracker : IDisposable
    {
        private int _DisposeCount;

        /// <summary>
        /// Initializes a disposable tracker.
        /// </summary>
        /// <param name="name">The tracker name used in assertions.</param>
        /// <param name="throwOnDispose"><c>true</c> to throw <see cref="InvalidOperationException"/> after recording disposal; otherwise <c>false</c>.</param>
        public DisposableTracker(string name, bool throwOnDispose = false)
        {
            Name = name;
            ThrowOnDispose = throwOnDispose;
        }

        /// <summary>
        /// Gets the tracker name used in assertions.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="Dispose"/> throws after recording disposal.
        /// </summary>
        public bool ThrowOnDispose { get; }

        /// <summary>
        /// Gets or sets an arbitrary ordering priority used by tests.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Gets the number of times <see cref="Dispose"/> has been called.
        /// </summary>
        public int DisposeCount => Volatile.Read(ref _DisposeCount);

        /// <summary>
        /// Gets a value indicating whether the tracker has been disposed at least once.
        /// </summary>
        public bool IsDisposed => DisposeCount > 0;

        /// <summary>
        /// Records one disposal operation.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="ThrowOnDispose"/> is <c>true</c>.</exception>
        public void Dispose()
        {
            Interlocked.Increment(ref _DisposeCount);
            if (ThrowOnDispose) throw new InvalidOperationException("Tracker " + Name + " was configured to throw on dispose.");
        }
    }
}
