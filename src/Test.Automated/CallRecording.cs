namespace Test.Automated
{
    /// <summary>
    /// Simulates an S3 call recording metadata object.
    /// </summary>
    public class CallRecording
    {
        /// <summary>
        /// Gets or sets the storage key for the call recording.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the size of the recording in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the timestamp associated with the recording.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Initializes a new call recording model.
        /// </summary>
        /// <param name="key">The storage key for the recording.</param>
        /// <param name="size">The recording size in bytes.</param>
        /// <param name="timestamp">The recording timestamp.</param>
        public CallRecording(string key, long size, DateTime timestamp)
        {
            Key = key;
            Size = size;
            Timestamp = timestamp;
        }
    }
}
