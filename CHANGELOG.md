# Changelog

## v1.0.0

- Sorted-list insertion/removal with a heap-backed implementation.
- Ordering configuration in `SelectorQueueBuilder<T>` and froze it at `Build()`.
- Preserve stable ordering across equal sort keys with a monotonic sequence tiebreaker.
- Public queue API including `Dispose`, `Enqueue`, `Dequeue`, `TryDequeue`, `Peek`, `TryPeek`, `Count`, and `Clear`.
- Automated test suite to exercise the library including ordering correctness, selector-failure behavior, concurrency invariants, and large-volume smoke coverage.
