namespace RVfamcamp.Models
{
    public class ReservationDetail
    {
        public int Id { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public int Conf { get; set; }

        public string CustomerName { get; set; }
        public string Email { get; set; }

        public int LotNum { get; set; }
    }
}
