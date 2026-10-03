# Changelog

## v1.0.2

- Maintenance release; no library API or behavior changes. The library itself has no NuGet dependencies.
- Test dependencies updated:
  - `Touchstone.Core`, `Touchstone.Cli`, `Touchstone.XunitAdapter`, `Touchstone.NunitAdapter` 0.1.12 -> 0.2.0
  - `NUnit` 4.3.2 -> 5.0.0, `NUnit.Analyzers` 4.7.0 -> 4.15.0, `NUnit3TestAdapter` 5.0.0 -> 6.3.0
  - `xunit.runner.visualstudio` 3.1.4 -> 4.0.0
  - `Microsoft.NET.Test.Sdk` 17.14.1 -> 18.10.1
  - `coverlet.collector` 6.0.4 -> 10.1.0

## v1.0.1

- Fixed: a key comparison that threw while the heap was reordered during `Dequeue`/`TryDequeue` dropped the head item. The sift-down path is now planned before any mutation, so the queue is left unchanged.
- Fixed: if one queued item threw from `Dispose()`, `Clear()`/`Dispose()` stopped and the remaining items were never disposed. Every item is now disposed. A single failure is rethrown unchanged, and multiple failures are reported together in an `AggregateException`.
- Reuse an internal path buffer for heap insert and remove to avoid per-operation list allocations.
- Test infrastructure migrated to Touchstone: `Test.Shared` (shared descriptors), `Test.Automated` (console), `Test.Xunit`, and `Test.Nunit` runners.
- Expanded positive and negative coverage: selector invocation and key capture, comparison failures, ownership and disposal edge cases, concurrent `Clear`/`Dispose` races, and additional ordering semantics.

## v1.0.0

- Sorted-list insertion/removal with a heap-backed implementation.
- Ordering configuration in `SelectorQueueBuilder<T>` and froze it at `Build()`.
- Preserve stable ordering across equal sort keys with a monotonic sequence tiebreaker.
- Public queue API including `Dispose`, `Enqueue`, `Dequeue`, `TryDequeue`, `Peek`, `TryPeek`, `Count`, and `Clear`.
- Automated test suite to exercise the library including ordering correctness, selector-failure behavior, concurrency invariants, and large-volume smoke coverage.
