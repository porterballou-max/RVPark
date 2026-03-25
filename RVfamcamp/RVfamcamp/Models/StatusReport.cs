namespace RVfamcamp.Models
{
    public class StatusReport
    {
        public List<ReservationDetail> Completed { get; set; } = new();
        public List<ReservationDetail> InProgress { get; set; } = new();
        public List<ReservationDetail> Upcoming { get; set; } = new();
    }
}
