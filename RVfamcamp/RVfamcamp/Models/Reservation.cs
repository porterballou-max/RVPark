namespace RVfamcamp.Models;

public class Reservation
{
    public int reservationId { get; set; }
    public DateTime startDate { get; set; }
    public DateTime endDate { get; set; }
    public int confirmationNumber { get; set; }

}