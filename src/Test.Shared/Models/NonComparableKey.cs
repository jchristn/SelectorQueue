namespace Test.Shared.Models
{
    /// <summary>
    /// Key type that intentionally does not implement <see cref="System.IComparable"/>.
    /// </summary>
    public sealed class NonComparableKey
    {
        /// <summary>
        /// Initializes a non-comparable key.
        /// </summary>
        /// <param name="value">The wrapped value.</param>
        public NonComparableKey(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the wrapped value.
        /// </summary>
        public int Value { get; }
    }
}
