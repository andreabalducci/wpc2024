using NStore.Core.InMemory;
using NStore.Core.Persistence;
using NStore.Core.Processing;
using NStore.Core.Streams;
using NStore.Domain;
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
    private readonly IRepository _repository;

    private readonly ParallelOptions _parallelOptions = new()
    {
        MaxDegreeOfParallelism = MaxDegreeOfParallelism
    };

    public StoreSample(ITestOutputHelper output)
    {
        _output = output;
        _persistence = new InMemoryPersistence();
        _streams = new StreamsFactory(_persistence);
        _repository = new Repository(new DefaultAggregateFactory(), _streams);
    }

    [Fact]
    public async Task SaleAggregate()
    {
        var wpc = StoreAggregate.CreateNew("WPC");
        wpc.AddTickets(1);
        await _repository.SaveAsync(wpc, "init");
        
        wpc.SaleTicket("T00001");
        await _repository.SaveAsync(wpc, Guid.NewGuid().ToString());

        await Dump();
    }

    [Fact]
    public async Task SaleOne()
    {
        var wpc = _streams.Open("WPC");
        await wpc.AppendAsync(new SaleStarted(_timeProvider.GetUtcNow()));
        await wpc.AppendAsync(new TicketSold("T00001", _timeProvider.GetUtcNow()));
        
        await Dump();
    }

    [Fact]
    public async Task SaleMany()
    {
        var wpc = _streams.Open("WPC");
        await wpc.AppendAsync(new SaleStarted(_timeProvider.GetUtcNow()));

        await Parallel.ForAsync(1, TicketCount + 1,
            _parallelOptions,
            async (i, cancellationToken) =>
            {
                await wpc.AppendAsync(new TicketSold($"T{i:D5}", _timeProvider.GetUtcNow()));
                await Task.Delay(_random.Next(10), cancellationToken);
            });

        await wpc.AppendAsync(new SaleEnded(_timeProvider.GetUtcNow()));


        var sum = await wpc.AggregateAsync<SumAsync>();
        _output.WriteLine("Sold {0} tickets in {1}ms", sum.Total, sum.Duration.Milliseconds);
        
        Assert.Equal(TicketCount, sum.Total);
        await Dump();
    }

    private async Task Dump()
    {
        _output.WriteLine("====================================");

        await _persistence.ReadAllAsync(0, new LambdaSubscription(c =>
        {
            if (c.IsFiller()) return Task.FromResult(true);

            _output.WriteLine(c.ToJson());
            return Task.FromResult(true);
        }));
        
        
        _output.WriteLine("====================================");
    }
}