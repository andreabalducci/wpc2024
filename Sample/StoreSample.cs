using NStore.Core.InMemory;
using NStore.Core.Persistence;
using NStore.Core.Streams;
using Xunit;
using Xunit.Abstractions;

namespace Sample;

public class StoreSample
{
    private readonly ITestOutputHelper _output;
    private readonly IPersistence _persistence;
    private readonly IStreamsFactory _streams;
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly Random _random = new Random();

    public StoreSample(ITestOutputHelper output)
    {
        _output = output;
        _persistence = new InMemoryPersistence();
        _streams = new StreamsFactory(_persistence);
    }

    [Fact]
    public async Task Create_and_Dump_Store()
    {
        var stream = _streams.Open("WPC");
        await stream.AppendAsync(new StoreCreated(_timeProvider.GetUtcNow()));

        await Parallel.ForAsync(1, 100, new ParallelOptions() {MaxDegreeOfParallelism = 4},
            async (i, cancellationToken) =>
            {
                await stream.AppendAsync(new TicketSold($"T{i}", _timeProvider.GetUtcNow()));
                await Task.Delay(_random.Next(10), cancellationToken);
            });

        await _persistence.ReadAllAsync(0, new LambdaSubscription(c =>
        {
            if (c.IsFiller()) return Task.FromResult(true);
            
            _output.WriteLine(c.ToJson());
            return Task.FromResult(true);
        }));
    }
}