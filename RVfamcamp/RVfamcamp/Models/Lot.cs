namespace RVfamcamp.Models;

public class Lot
{
    public int LotId { get; set; }
    public int LotNumber { get; set; }
    public bool IsOccupied { get; set; }
    public int LotType { get; set; }
}