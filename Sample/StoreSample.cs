using NStore.Core.InMemory;
using NStore.Core.Persistence;
using NStore.Core.Processing;
using NStore.Core.Streams;
using Xunit;
using Xunit.Abstractions;

namespace Sample;

public class StoreSample
{
    private const int MaxDegreeOfParallelism = 16;
    private const int TicketCount = 10;

    private readonly ITestOutputHelper _output;
    private readonly IPersistence _persistence;
    private readonly IStreamsFactory _streams;
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly Random _random = new();

    private readonly ParallelOptions _parallelOptions = new()
    {
        MaxDegreeOfParallelism = MaxDegreeOfParallelism
    };

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
        await stream.AppendAsync(new SaleStarted(_timeProvider.GetUtcNow()));

        await Parallel.ForAsync(1, TicketCount + 1,
            _parallelOptions,
            async (i, cancellationToken) =>
            {
                await stream.AppendAsync(new TicketSold($"T{i}", _timeProvider.GetUtcNow()));
                await Task.Delay(_random.Next(10), cancellationToken);
            });

        await stream.AppendAsync(new SaleEnded(_timeProvider.GetUtcNow()));


        var sum = await stream.AggregateAsync<SumAsync>();
        _output.WriteLine("Sold {0} tickets in {1}ms", sum.Total, sum.Duration.Milliseconds);
        
        Assert.Equal(TicketCount, sum.Total);
        await Dump();
    }

    private async Task Dump()
    {
        await _persistence.ReadAllAsync(0, new LambdaSubscription(c =>
        {
            if (c.IsFiller()) return Task.FromResult(true);

            _output.WriteLine($"{c.Payload.GetType().Name} => {c.ToJson()}");
            return Task.FromResult(true);
        }));
    }
}