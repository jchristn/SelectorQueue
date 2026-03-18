namespace Test.Automated.Suites
{
    /// <summary>
    /// Helper model used by thread-safety stability tests.
    /// </summary>
    internal sealed class ThreadSafetyStableItem
    {
        /// <summary>
        /// Initializes a helper model used by thread-safety stability tests.
        /// </summary>
        /// <param name="producerId">The producing worker identifier.</param>
        /// <param name="sequenceInProducer">The item's sequence within one producer.</param>
        public ThreadSafetyStableItem(int producerId, int sequenceInProducer)
        {
            ProducerId = producerId;
            SequenceInProducer = sequenceInProducer;
        }

        /// <summary>
        /// Gets the producing worker identifier.
        /// </summary>
        public int ProducerId { get; }

        /// <summary>
        /// Gets the sequence of the item within one producer.
        /// </summary>
        public int SequenceInProducer { get; }

        /// <summary>
        /// Gets the shared equal-key priority used by the test.
        /// </summary>
        public int Priority => 1;
    }
}
