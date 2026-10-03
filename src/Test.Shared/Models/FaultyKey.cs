namespace Test.Shared.Models
{
    using System;

    /// <summary>
    /// Integer key whose comparisons can be made to throw through a shared <see cref="FaultSwitch"/>.
    /// </summary>
    public sealed class FaultyKey : IComparable<FaultyKey>
    {
        /// <summary>
        /// Initializes a faulty key.
        /// </summary>
        /// <param name="value">The ordering value.</param>
        /// <param name="fault">The switch that controls comparison failures.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="fault"/> is null.</exception>
        public FaultyKey(int value, FaultSwitch fault)
        {
            Value = value;
            Fault = fault ?? throw new ArgumentNullException(nameof(fault));
        }

        /// <summary>
        /// Gets the ordering value.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Gets the switch that controls comparison failures.
        /// </summary>
        public FaultSwitch Fault { get; }

        /// <summary>
        /// Compares by <see cref="Value"/>, throwing first if the switch injects a fault. Null sorts first.
        /// </summary>
        /// <param name="other">The other key.</param>
        /// <returns>A signed comparison result.</returns>
        /// <exception cref="InjectedFaultException">Thrown when the switch injects a fault.</exception>
        public int CompareTo(FaultyKey? other)
        {
            Fault.MaybeThrow();
            if (other == null) return 1;
            return Value.CompareTo(other.Value);
        }
    }
}
