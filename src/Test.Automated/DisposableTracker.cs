namespace Test.Automated
{
    /// <summary>
    /// Tracks disposal events for tests that verify queue ownership semantics.
    /// </summary>
    public sealed class DisposableTracker : IDisposable
    {
        /// <summary>
        /// Initializes a disposable tracker.
        /// </summary>
        /// <param name="name">The tracker name used in assertions.</param>
        public DisposableTracker(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the tracker name used in assertions.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the number of times <see cref="Dispose"/> has been called.
        /// </summary>
        public int DisposeCount { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the tracker has been disposed at least once.
        /// </summary>
        public bool IsDisposed => DisposeCount > 0;

        /// <summary>
        /// Records one disposal operation.
        /// </summary>
        public void Dispose()
        {
            DisposeCount++;
        }
    }
}
