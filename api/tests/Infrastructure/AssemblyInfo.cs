using Xunit;

// The endpoint test classes share one PostgreSQL database and reset it per test.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
