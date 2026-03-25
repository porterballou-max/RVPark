using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Configuration;
using RVfamcamp.Models;
using RVfamcamp.Services;
using System;

public class DetailsModel(DatabaseStatements db) : PageModel
{
    public required Reservation Reservation { get; set; }
    public List<Lot>? Lots { get; set; }
    public int ReservationId { get; set; }

    public void OnGet(int id)
    {
        ReservationId = id;

        // Get reservation information
        Reservation = db.GetReservationById(id);

        // Get all lots associated with reservation
        Lots = db.GetLotsByReservationId(id);

            
    }
}