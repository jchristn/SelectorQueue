namespace Test.Shared
{
    using System;

    /// <summary>
    /// Thrown by <see cref="TestAssert"/> when a test assertion fails.
    /// </summary>
    public sealed class TestAssertionException : Exception
    {
        /// <summary>
        /// Initializes a new assertion failure.
        /// </summary>
        /// <param name="message">The assertion failure message.</param>
        public TestAssertionException(string message) : base(message)
        {
        }
    }
}
