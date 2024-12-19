namespace Ticket.Modules.Services;

public class DynamicPricingService
{
    public double CalculatePrice(double basePrice, int remainingTickets, int totalTickets, DateTime eventDate, DateTime bookingDate, bool isSpecialEvent)
    {
        double demandMultiplier = GetDemandMultiplier(remainingTickets, totalTickets);
        double timeMultiplier = GetTimeMultiplier(eventDate, bookingDate);
        double eventMultiplier = GetEventMultiplier(isSpecialEvent);

        return basePrice * demandMultiplier * timeMultiplier * eventMultiplier;
    }

    private double GetDemandMultiplier(int remainingTickets, int totalTickets)
    {
        if (remainingTickets <= 0.2 * totalTickets) return 1.5; // Giá tăng 50%
        return -0.1; // Giảm 10% nếu còn nhiều vé
    }

    private double GetTimeMultiplier(DateTime eventDate, DateTime bookingDate)
    {
        var daysToEvent = (eventDate - bookingDate).TotalDays;
        if (daysToEvent <= 3) return 2.0; // Giá gấp đôi
        if (daysToEvent <= 7) return 1.5; // Giá tăng 50%
        return 1.0; // Giá không đổi
    }

    private double GetEventMultiplier(bool isSpecialEvent)
    {
        return isSpecialEvent ? 1.8 : 1.0; // Giá tăng 80% nếu là sự kiện đặc biệt
    }
}
