namespace Test.Shared
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Runner-agnostic assertion helpers. Every failure throws <see cref="TestAssertionException"/>.
    /// </summary>
    public static class TestAssert
    {
        /// <summary>
        /// Asserts that a condition is true.
        /// </summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="label">An optional assertion label.</param>
        /// <exception cref="TestAssertionException">Thrown when the condition is false.</exception>
        public static void True(bool condition, string? label = null)
        {
            if (!condition) Fail((label ?? "Condition") + " was false");
        }

        /// <summary>
        /// Asserts that a condition is false.
        /// </summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="label">An optional assertion label.</param>
        /// <exception cref="TestAssertionException">Thrown when the condition is true.</exception>
        public static void False(bool condition, string? label = null)
        {
            if (condition) Fail((label ?? "Condition") + " was true");
        }

        /// <summary>
        /// Asserts that a value is null.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <param name="label">An optional assertion label.</param>
        /// <exception cref="TestAssertionException">Thrown when the value is not null.</exception>
        public static void Null(object? value, string? label = null)
        {
            if (value != null) Fail((label ?? "Value") + ": expected null but got <" + value + ">");
        }

        /// <summary>
        /// Asserts that a value is not null.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <param name="label">An optional assertion label.</param>
        /// <exception cref="TestAssertionException">Thrown when the value is null.</exception>
        public static void NotNull(object? value, string? label = null)
        {
            if (value == null) Fail((label ?? "Value") + " was null");
        }

        /// <summary>
        /// Asserts that two values are equal using the default equality comparer.
        /// </summary>
        /// <typeparam name="TVal">The compared value type.</typeparam>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="label">An optional assertion label.</param>
        /// <exception cref="TestAssertionException">Thrown when the values differ.</exception>
        public static void Equal<TVal>(TVal expected, TVal actual, string? label = null)
        {
            if (!EqualityComparer<TVal>.Default.Equals(expected, actual))
            {
                Fail(Prefix(label) + "expected <" + expected + "> but got <" + actual + ">");
            }
        }

        /// <summary>
        /// Asserts that two references point to the same object.
        /// </summary>
        /// <param name="expected">The expected reference.</param>
        /// <param name="actual">The actual reference.</param>
        /// <param name="label">An optional assertion label.</param>
        /// <exception cref="TestAssertionException">Thrown when the references differ.</exception>
        public static void Same(object? expected, object? actual, string? label = null)
        {
            if (!ReferenceEquals(expected, actual)) Fail(Prefix(label) + "expected the same instance");
        }

        /// <summary>
        /// Asserts that a value is greater than a threshold.
        /// </summary>
        /// <typeparam name="TVal">The compared value type.</typeparam>
        /// <param name="value">The tested value.</param>
        /// <param name="threshold">The exclusive lower bound.</param>
        /// <param name="label">An optional assertion label.</param>
        /// <exception cref="TestAssertionException">Thrown when the value is not greater than the threshold.</exception>
        public static void GreaterThan<TVal>(TVal value, TVal threshold, string? label = null) where TVal : IComparable<TVal>
        {
            if (value.CompareTo(threshold) <= 0) Fail(Prefix(label) + "expected <" + value + "> to be greater than <" + threshold + ">");
        }

        /// <summary>
        /// Asserts that a value is greater than or equal to a threshold.
        /// </summary>
        /// <typeparam name="TVal">The compared value type.</typeparam>
        /// <param name="value">The tested value.</param>
        /// <param name="threshold">The inclusive lower bound.</param>
        /// <param name="label">An optional assertion label.</param>
        /// <exception cref="TestAssertionException">Thrown when the value is less than the threshold.</exception>
        public static void GreaterThanOrEqual<TVal>(TVal value, TVal threshold, string? label = null) where TVal : IComparable<TVal>
        {
            if (value.CompareTo(threshold) < 0) Fail(Prefix(label) + "expected <" + value + "> to be greater than or equal to <" + threshold + ">");
        }

        /// <summary>
        /// Asserts that a value is less than or equal to a threshold.
        /// </summary>
        /// <typeparam name="TVal">The compared value type.</typeparam>
        /// <param name="value">The tested value.</param>
        /// <param name="threshold">The inclusive upper bound.</param>
        /// <param name="label">An optional assertion label.</param>
        /// <exception cref="TestAssertionException">Thrown when the value exceeds the threshold.</exception>
        public static void LessThanOrEqual<TVal>(TVal value, TVal threshold, string? label = null) where TVal : IComparable<TVal>
        {
            if (value.CompareTo(threshold) > 0) Fail(Prefix(label) + "expected <" + value + "> to be less than or equal to <" + threshold + ">");
        }

        /// <summary>
        /// Asserts that two sequences contain equal elements in the same order.
        /// </summary>
        /// <typeparam name="TVal">The element type.</typeparam>
        /// <param name="expected">The expected sequence.</param>
        /// <param name="actual">The actual sequence.</param>
        /// <param name="label">An optional assertion label.</param>
        /// <exception cref="TestAssertionException">Thrown when the sequences differ.</exception>
        public static void SequenceEqual<TVal>(IReadOnlyList<TVal> expected, IReadOnlyList<TVal> actual, string? label = null)
        {
            if (expected.Count != actual.Count)
            {
                Fail(Prefix(label) + "expected " + expected.Count + " elements but got " + actual.Count);
            }

            for (int i = 0; i < expected.Count; i++)
            {
                if (!EqualityComparer<TVal>.Default.Equals(expected[i], actual[i]))
                {
                    Fail(Prefix(label) + "index " + i + " expected <" + expected[i] + "> but got <" + actual[i] + ">");
                }
            }
        }

        /// <summary>
        /// Asserts that the action throws exactly the specified exception type (or a derived type).
        /// </summary>
        /// <typeparam name="TException">The expected exception type.</typeparam>
        /// <param name="action">The action expected to throw.</param>
        /// <param name="label">An optional assertion label.</param>
        /// <returns>The caught exception.</returns>
        /// <exception cref="TestAssertionException">Thrown when no exception, or an exception of another type, is thrown.</exception>
        public static TException Throws<TException>(Action action, string? label = null) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException expected)
            {
                return expected;
            }
            catch (Exception other)
            {
                Fail(Prefix(label) + "expected " + typeof(TException).Name + " but got " + other.GetType().Name + ": " + other.Message);
            }

            Fail(Prefix(label) + "expected " + typeof(TException).Name + " but no exception was thrown");
            return null!;
        }

        /// <summary>
        /// Fails the current test unconditionally.
        /// </summary>
        /// <param name="message">The failure message.</param>
        /// <exception cref="TestAssertionException">Always thrown.</exception>
        public static void Fail(string message)
        {
            throw new TestAssertionException("Assertion failed: " + message);
        }

        private static string Prefix(string? label)
        {
            return label != null ? label + ": " : "";
        }
    }
}
