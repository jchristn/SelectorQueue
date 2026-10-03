using Xunit;

// Performance, stress, and allocation tests measure process-wide resources, so run serially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
