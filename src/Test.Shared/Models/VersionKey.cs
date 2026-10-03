namespace Test.Shared.Models
{
    using System;

    /// <summary>
    /// Custom comparable key that orders by major then minor version.
    /// </summary>
    public sealed class VersionKey : IComparable<VersionKey>
    {
        /// <summary>
        /// Initializes a version key.
        /// </summary>
        /// <param name="major">The major version.</param>
        /// <param name="minor">The minor version.</param>
        public VersionKey(int major, int minor)
        {
            Major = major;
            Minor = minor;
        }

        /// <summary>
        /// Gets the major version.
        /// </summary>
        public int Major { get; }

        /// <summary>
        /// Gets the minor version.
        /// </summary>
        public int Minor { get; }

        /// <summary>
        /// Compares this key to another key by major and then minor version. Null sorts first.
        /// </summary>
        /// <param name="other">The other key.</param>
        /// <returns>A signed comparison result.</returns>
        public int CompareTo(VersionKey? other)
        {
            if (other == null) return 1;
            int result = Major.CompareTo(other.Major);
            if (result != 0) return result;
            return Minor.CompareTo(other.Minor);
        }
    }
}
