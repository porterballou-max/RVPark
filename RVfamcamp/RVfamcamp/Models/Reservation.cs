namespace RVfamcamp.Models;

public class Reservation
{
    public int ReservationId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int ConfirmationNumber { get; set; }
    public int UserAccountId { get; set; }
}