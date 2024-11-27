using NStore.Domain;

namespace Sample;

public class StoreAggregate : Aggregate<StoreAggregate.InternalState>
{
    public class InternalState
    {
        public int Available { get; private set; }
        public int Sold { get; private set; }

        public bool CanSale(int noOfTickets)
        {
            return Available - Sold >= noOfTickets;
        }

        private void On(TicketSold e)
        {
            this.Sold++;
        }
        
        private void  On(CreateTicketsForSale e)
        {
            this.Available += e.TicketCount;
        }
    }
    
    public static StoreAggregate CreateNew(string id)
    {
        var store = new StoreAggregate();
        store.Init(id);
        return store;
    }

    public void AddTickets(int ticketCount)
    {
        this.Emit(new CreateTicketsForSale(ticketCount));
    }
    
    public void SaleTicket(string ticketId)
    {
        if (this.State.CanSale(1))
        {
            this.Emit(new TicketSold(ticketId, TimeProvider.System.GetUtcNow()));
        }
        else
        {
            throw new System.Exception("No more tickets available");
        }
    }
}