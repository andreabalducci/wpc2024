namespace Sample;

public record StoreCreated(DateTimeOffset CreatedAt);
public record TicketSold(String TicketId, DateTimeOffset SoldAt);
