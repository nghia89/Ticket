namespace Ticket.Modules.TicketBooking;

public interface ITicketBookingService
{
    Task<bool> BookTicket(string trainId, DateTime date, string seatNumber, string userId);
    Task<List<Shared.Models.Ticket>> GetTicket(string trainId, DateTime date);
}