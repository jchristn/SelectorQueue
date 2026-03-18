namespace Test.Automated
{
    using System.Diagnostics;

    /// <summary>
    /// Abstract base class for test suites. Provides assertion helpers and test execution wrappers.
    /// </summary>
    public abstract class TestSuite
    {
        #region Public-Members

        /// <summary>
        /// Gets the display name of this test suite.
        /// </summary>
        public abstract string Name { get; }

        #endregion

        #region Private-Members

        private List<TestResult> _Results = new List<TestResult>();

        #endregion

        #region Public-Methods

        /// <summary>
        /// Runs all tests in this suite and returns the collected results.
        /// </summary>
        /// <returns>The collected test results for this suite.</returns>
        public async Task<List<TestResult>> RunAsync()
        {
            _Results = new List<TestResult>();
            await RunTestsAsync().ConfigureAwait(false);
            return _Results;
        }

        #endregion

        #region Protected-Methods

        /// <summary>
        /// Overrides define and run tests using <see cref="RunTest(string, Func{Task})"/> or <see cref="RunTest(string, Action)"/>.
        /// </summary>
        /// <returns>A task that completes when all suite tests have run.</returns>
        protected abstract Task RunTestsAsync();

        /// <summary>
        /// Wraps an asynchronous test in timing and exception handling.
        /// </summary>
        /// <param name="name">The display name of the test.</param>
        /// <param name="action">The asynchronous test body.</param>
        /// <returns>A task that completes when the test has been recorded.</returns>
        protected async Task RunTest(string name, Func<Task> action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            TestResult result = new TestResult { Name = name };

            try
            {
                await action().ConfigureAwait(false);
                stopwatch.Stop();
                result.Passed = true;
                result.ElapsedMs = stopwatch.ElapsedMilliseconds;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("  PASS  ");
                Console.ResetColor();
                Console.WriteLine(name + " (" + result.ElapsedMs + "ms)");
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                result.Passed = false;
                result.ElapsedMs = stopwatch.ElapsedMilliseconds;
                result.Exception = exception;
                result.Message = exception.GetType().Name + " - " + exception.Message;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  FAIL  ");
                Console.ResetColor();
                Console.WriteLine(name + " (" + result.ElapsedMs + "ms)");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("         " + result.Message);
                Console.ResetColor();
            }

            _Results.Add(result);
        }

        /// <summary>
        /// Wraps a synchronous test in timing and exception handling.
        /// </summary>
        /// <param name="name">The display name of the test.</param>
        /// <param name="action">The synchronous test body.</param>
        /// <returns>A task that completes when the test has been recorded.</returns>
        protected async Task RunTest(string name, Action action)
        {
            await RunTest(name, () =>
            {
                action();
                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Asserts that a condition is true.
        /// </summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="message">The failure message to use if the assertion fails.</param>
        protected void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception("Assertion failed: " + message);
        }

        /// <summary>
        /// Asserts that two values are equal.
        /// </summary>
        /// <typeparam name="TVal">The compared value type.</typeparam>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="label">An optional assertion label.</param>
        protected void AssertEqual<TVal>(TVal expected, TVal actual, string? label = null)
        {
            if (!EqualityComparer<TVal>.Default.Equals(expected, actual))
            {
                string message = label != null
                    ? label + ": expected <" + expected + "> but got <" + actual + ">"
                    : "Expected <" + expected + "> but got <" + actual + ">";
                throw new Exception("Assertion failed: " + message);
            }
        }

        /// <summary>
        /// Asserts that a value is not null.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <param name="label">An optional assertion label.</param>
        protected void AssertNotNull(object? value, string? label = null)
        {
            if (value == null)
            {
                string message = label != null
                    ? label + " was null"
                    : "Value was null";
                throw new Exception("Assertion failed: " + message);
            }
        }

        /// <summary>
        /// Asserts that a condition is true.
        /// </summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="label">An optional assertion label.</param>
        protected void AssertTrue(bool condition, string? label = null)
        {
            if (!condition)
            {
                string message = label != null
                    ? label + " was false"
                    : "Condition was false";
                throw new Exception("Assertion failed: " + message);
            }
        }

        /// <summary>
        /// Asserts that a condition is false.
        /// </summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="label">An optional assertion label.</param>
        protected void AssertFalse(bool condition, string? label = null)
        {
            if (condition)
            {
                string message = label != null
                    ? label + " was true"
                    : "Condition was true";
                throw new Exception("Assertion failed: " + message);
            }
        }

        /// <summary>
        /// Asserts that the given action throws the specified exception type.
        /// </summary>
        /// <typeparam name="TException">The expected exception type.</typeparam>
        /// <param name="action">The action expected to throw.</param>
        /// <param name="label">An optional assertion label.</param>
        protected void AssertThrows<TException>(Action action, string? label = null) where TException : Exception
        {
            try
            {
                action();
                string message = label != null
                    ? label + ": expected " + typeof(TException).Name + " but no exception was thrown"
                    : "Expected " + typeof(TException).Name + " but no exception was thrown";
                throw new Exception("Assertion failed: " + message);
            }
            catch (TException)
            {
            }
        }

        /// <summary>
        /// Asserts that two values are not equal.
        /// </summary>
        /// <typeparam name="TVal">The compared value type.</typeparam>
        /// <param name="unexpected">The value that must not match.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="label">An optional assertion label.</param>
        protected void AssertNotEqual<TVal>(TVal unexpected, TVal actual, string? label = null)
        {
            if (EqualityComparer<TVal>.Default.Equals(unexpected, actual))
            {
                string message = label != null
                    ? label + ": values should not be equal but both were <" + actual + ">"
                    : "Values should not be equal but both were <" + actual + ">";
                throw new Exception("Assertion failed: " + message);
            }
        }

        /// <summary>
        /// Asserts that a collection has the expected count.
        /// </summary>
        /// <typeparam name="TVal">The collection item type.</typeparam>
        /// <param name="expected">The expected item count.</param>
        /// <param name="collection">The collection being checked.</param>
        /// <param name="label">An optional assertion label.</param>
        protected void AssertCount<TVal>(int expected, ICollection<TVal> collection, string? label = null)
        {
            if (collection.Count != expected)
            {
                string message = label != null
                    ? label + ": expected count <" + expected + "> but got <" + collection.Count + ">"
                    : "Expected count <" + expected + "> but got <" + collection.Count + ">";
                throw new Exception("Assertion failed: " + message);
            }
        }

        /// <summary>
        /// Asserts that a value is greater than another.
        /// </summary>
        /// <typeparam name="TVal">The compared value type.</typeparam>
        /// <param name="value">The tested value.</param>
        /// <param name="threshold">The lower bound that <paramref name="value"/> must exceed.</param>
        /// <param name="label">An optional assertion label.</param>
        protected void AssertGreaterThan<TVal>(TVal value, TVal threshold, string? label = null) where TVal : IComparable<TVal>
        {
            if (value.CompareTo(threshold) <= 0)
            {
                string message = label != null
                    ? label + ": expected <" + value + "> to be greater than <" + threshold + ">"
                    : "Expected <" + value + "> to be greater than <" + threshold + ">";
                throw new Exception("Assertion failed: " + message);
            }
        }

        /// <summary>
        /// Asserts that a value is less than or equal to another.
        /// </summary>
        /// <typeparam name="TVal">The compared value type.</typeparam>
        /// <param name="value">The tested value.</param>
        /// <param name="threshold">The upper bound that <paramref name="value"/> must not exceed.</param>
        /// <param name="label">An optional assertion label.</param>
        protected void AssertLessThanOrEqual<TVal>(TVal value, TVal threshold, string? label = null) where TVal : IComparable<TVal>
        {
            if (value.CompareTo(threshold) > 0)
            {
                string message = label != null
                    ? label + ": expected <" + value + "> to be less than or equal to <" + threshold + ">"
                    : "Expected <" + value + "> to be less than or equal to <" + threshold + ">";
                throw new Exception("Assertion failed: " + message);
            }
        }

        /// <summary>
        /// Asserts that a value is greater than or equal to another.
        /// </summary>
        /// <typeparam name="TVal">The compared value type.</typeparam>
        /// <param name="value">The tested value.</param>
        /// <param name="threshold">The lower bound that <paramref name="value"/> must meet.</param>
        /// <param name="label">An optional assertion label.</param>
        protected void AssertGreaterThanOrEqual<TVal>(TVal value, TVal threshold, string? label = null) where TVal : IComparable<TVal>
        {
            if (value.CompareTo(threshold) < 0)
            {
                string message = label != null
                    ? label + ": expected <" + value + "> to be greater than or equal to <" + threshold + ">"
                    : "Expected <" + value + "> to be greater than or equal to <" + threshold + ">";
                throw new Exception("Assertion failed: " + message);
            }
        }

        #endregion
    }
}
