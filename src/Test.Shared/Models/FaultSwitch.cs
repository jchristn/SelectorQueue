namespace Test.Shared.Models
{
    using System;

    /// <summary>
    /// Controls when <see cref="FaultyKey"/> comparisons throw. Not thread-safe; intended for single-threaded tests.
    /// </summary>
    public sealed class FaultSwitch
    {
        private readonly Random? _Random;
        private readonly double _Probability;

        /// <summary>
        /// Initializes a switch that throws on every comparison while <see cref="Armed"/> is <c>true</c>.
        /// </summary>
        public FaultSwitch()
        {
            _Probability = 1.0;
        }

        /// <summary>
        /// Initializes a switch that, while armed, throws on a random fraction of comparisons.
        /// </summary>
        /// <param name="seed">The random seed.</param>
        /// <param name="probability">The chance, from 0.0 (never) to 1.0 (always), that an armed comparison throws.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="probability"/> is outside 0.0 to 1.0.</exception>
        public FaultSwitch(int seed, double probability)
        {
            if (probability < 0.0 || probability > 1.0) throw new ArgumentOutOfRangeException(nameof(probability), "Probability must be between 0.0 and 1.0.");
            _Random = new Random(seed);
            _Probability = probability;
        }

        /// <summary>
        /// Gets or sets a value indicating whether comparisons may throw. Default is <c>false</c>.
        /// </summary>
        public bool Armed { get; set; }

        /// <summary>
        /// Gets the number of faults injected so far.
        /// </summary>
        public int FaultCount { get; private set; }

        /// <summary>
        /// Throws <see cref="InjectedFaultException"/> when the switch decides this comparison should fail.
        /// </summary>
        /// <exception cref="InjectedFaultException">Thrown when a fault is injected.</exception>
        public void MaybeThrow()
        {
            if (!Armed) return;
            if (_Random != null && _Random.NextDouble() >= _Probability) return;

            FaultCount++;
            throw new InjectedFaultException("Injected comparison fault #" + FaultCount + ".");
        }
    }
}
