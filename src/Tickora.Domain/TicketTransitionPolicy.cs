namespace Tickora.Domain;

public static class TicketTransitionPolicy
{
    public static bool CanTransition(TicketStatus from, TicketStatus to, bool staff) => (from, to) switch
    {
        (TicketStatus.Open, TicketStatus.InProgress) => staff,
        (TicketStatus.Open or TicketStatus.InProgress, TicketStatus.WaitingForCustomer) => staff,
        (TicketStatus.Open or TicketStatus.InProgress or TicketStatus.WaitingForCustomer, TicketStatus.Resolved) => staff,
        (TicketStatus.WaitingForCustomer, TicketStatus.InProgress) => true,
        (TicketStatus.Resolved, TicketStatus.Closed or TicketStatus.Open) => true,
        (TicketStatus.Closed, TicketStatus.Open) => true,
        _ => false
    };
}
