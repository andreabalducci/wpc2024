namespace Sample;

public record SaleStarted(DateTimeOffset StartedAt);
public record SaleEnded(DateTimeOffset EndedAt);
public record TicketSold(String TicketId, DateTimeOffset SoldAt);
