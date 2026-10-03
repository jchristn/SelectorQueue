namespace Test.Shared
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Touchstone.Core;

    /// <summary>
    /// Factory helpers for building Touchstone test case descriptors.
    /// </summary>
    public static class TestCases
    {
        /// <summary>
        /// Creates a descriptor for a synchronous test body.
        /// </summary>
        /// <param name="suiteId">The owning suite identifier.</param>
        /// <param name="caseId">The case identifier, unique within the suite.</param>
        /// <param name="displayName">The human-readable test name.</param>
        /// <param name="body">The synchronous test body. Assertions fail by throwing.</param>
        /// <returns>The test case descriptor.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="body"/> is null.</exception>
        public static TestCaseDescriptor Sync(string suiteId, string caseId, string displayName, Action body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));

            return new TestCaseDescriptor(
                suiteId: suiteId,
                caseId: caseId,
                displayName: displayName,
                executeAsync: ct =>
                {
                    ct.ThrowIfCancellationRequested();
                    body();
                    return Task.CompletedTask;
                });
        }

        /// <summary>
        /// Creates a descriptor for an asynchronous test body.
        /// </summary>
        /// <param name="suiteId">The owning suite identifier.</param>
        /// <param name="caseId">The case identifier, unique within the suite.</param>
        /// <param name="displayName">The human-readable test name.</param>
        /// <param name="body">The asynchronous test body. Assertions fail by throwing.</param>
        /// <returns>The test case descriptor.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="body"/> is null.</exception>
        public static TestCaseDescriptor Async(string suiteId, string caseId, string displayName, Func<CancellationToken, Task> body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));

            return new TestCaseDescriptor(
                suiteId: suiteId,
                caseId: caseId,
                displayName: displayName,
                executeAsync: body);
        }
    }
}
