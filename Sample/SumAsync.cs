namespace Sample;

public class SumAsync
{
    private DateTimeOffset _start;
    private DateTimeOffset _end;
    public int Total { get; private set; }
    public TimeSpan Duration => _end - _start;
    private ValueTask On(SaleStarted data)
    {
        _start = data.StartedAt;
        return ValueTask.CompletedTask;
    }
    
    private ValueTask On(SaleEnded data)
    {
        _end = data.EndedAt;
        return ValueTask.CompletedTask;
    }
    
    private ValueTask On(TicketSold _)
    {
        this.Total += 1;
        return ValueTask.CompletedTask;
    }
}