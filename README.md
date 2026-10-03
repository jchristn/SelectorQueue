# SelectorQueue

<p align="center">
  <img src="https://raw.githubusercontent.com/jchristn/selectorqueue/main/assets/icon.png" alt="SelectorQueue icon" width="256" height="256" />
</p>

`SelectorQueue` is a thread-safe, stable ordered queue for .NET. It accepts items in nondeterministic arrival order and dequeues them in a frozen sort order defined up front through a builder.

## What Changed

Version `1.0.0` ships with:

- a binary heap instead of sorted-list insertion/removal
- immutable ordering configuration built before queue use
- stable ordering via an internal sequence tiebreaker
- a reduced public API focused on core queue behavior

## Usage

```csharp
using SelectorQueue;

public sealed class CallRecording
{
    public string Key { get; init; } = string.Empty;
    public long Size { get; init; }
    public DateTime Timestamp { get; init; }
}

SelectorQueue<CallRecording> queue = SelectorQueue
    .OrderBy<CallRecording, DateTime>(x => x.Timestamp)
    .ThenByDescending<long>(x => x.Size)
    .ThenBy<string>(x => x.Key)
    .Build();

queue.Enqueue(new CallRecording { Key = "call_003.wav", Size = 50_000, Timestamp = DateTime.Parse("2025-06-15T14:00:00Z") });
queue.Enqueue(new CallRecording { Key = "call_001.wav", Size = 30_000, Timestamp = DateTime.Parse("2025-06-15T12:00:00Z") });
queue.Enqueue(new CallRecording { Key = "call_002.wav", Size = 40_000, Timestamp = DateTime.Parse("2025-06-15T13:00:00Z") });

while (queue.TryDequeue(out CallRecording recording))
{
    Console.WriteLine($"{recording.Key} {recording.Timestamp:o} {recording.Size}");
}
```

If you do not need explicit ordering, use `SelectorQueue.Create<T>()` for a FIFO queue.

### IDisposable Example

```csharp
using SelectorQueue;

public sealed class WorkItem : IDisposable
{
    public WorkItem(string name, int priority)
    {
        Name = name;
        Priority = priority;
    }

    public string Name { get; }

    public int Priority { get; }

    public void Dispose()
    {
        Console.WriteLine("Disposed " + Name);
    }
}

using SelectorQueue<WorkItem> queue = SelectorQueue
    .OrderBy<WorkItem, int>(x => x.Priority)
    .Build();

queue.Enqueue(new WorkItem("low", 10));
queue.Enqueue(new WorkItem("high", 1));

WorkItem next = queue.Dequeue();
try
{
    Console.WriteLine("Processing " + next.Name);
}
finally
{
    next.Dispose();
}

// Any remaining queued WorkItem instances are disposed automatically
// when the queue is cleared or disposed.
```

## Supported API

Ordering is configured on `SelectorQueueBuilder<T>` and frozen when `Build()` is called.

- `OrderBy`
- `OrderByDescending`
- `ThenBy`
- `ThenByDescending`
- `Build`

Queue operations are exposed on `SelectorQueue<T>`.

- `Enqueue`
- `Dequeue`
- `TryDequeue`
- `Peek`
- `TryPeek`
- `Count`
- `Clear`
- `Dispose`

## Contract

- `Enqueue` is `O(log n)`.
- `Dequeue` is `O(log n)`.
- `Peek` is `O(1)`.
- Equal ordering keys preserve enqueue order.
- The queue owns items while they remain enqueued.
- `Clear()` removes queued items but keeps the queue's ordering configuration for its lifetime.
- `Clear()` disposes queued values that implement `IDisposable`.
- `Dispose()` disposes queued values that implement `IDisposable` and permanently closes the queue.
- After `Dispose()`, all queue operations throw `ObjectDisposedException`.
- `Dequeue()` and successful `TryDequeue()` transfer ownership of the returned item back to the caller.
- Empty `Dequeue()` and `Peek()` throw `InvalidOperationException`.
- Empty `TryDequeue()` and `TryPeek()` return `false` and set the out value to `default`.
- Selector delegates run before insertion. If a selector throws during `Enqueue`, the queue is unchanged.
- Null keys follow `Comparer<TKey>.Default` semantics. For reference and nullable types, null sorts before non-null in ascending order.

## Thread Safety

All queue operations are linearizable and safe for concurrent producers and consumers. Heap mutation occurs inside a single lock.

## Testing

Tests are written once as [Touchstone](https://github.com/jchristn/touchstone) descriptors in `src/Test.Shared` and run through any of three runners:

```bash
# Console runner (colored table output, optional JSON export)
dotnet run --project src/Test.Automated -- --results results.json

# xUnit runner
dotnet test src/Test.Xunit

# NUnit runner
dotnet test src/Test.Nunit
```

| Project | Purpose |
|---------|---------|
| `Test.Shared` | Central source of truth for all test suites (depends only on `Touchstone.Core`) |
| `Test.Automated` | Console runner using `Touchstone.Cli` |
| `Test.Xunit` | xUnit runner using `Touchstone.XunitAdapter` |
| `Test.Nunit` | NUnit runner using `Touchstone.NunitAdapter` |

## Framework

The package targets `net8.0` and `net10.0`.

## Version History

See [CHANGELOG.md](CHANGELOG.md).

## Attribution

Icon attribution: https://www.flaticon.com/free-icon/sort_306422
