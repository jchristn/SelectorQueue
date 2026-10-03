namespace Test.Shared.Models
{
    using System;

    /// <summary>
    /// Thrown by <see cref="FaultyKey"/> when its <see cref="FaultSwitch"/> injects a comparison failure.
    /// </summary>
    public sealed class InjectedFaultException : Exception
    {
        /// <summary>
        /// Initializes a new injected fault.
        /// </summary>
        /// <param name="message">The failure message.</param>
        public InjectedFaultException(string message) : base(message)
        {
        }
    }
}
