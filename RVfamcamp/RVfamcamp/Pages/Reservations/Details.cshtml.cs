using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Configuration;
using RVfamcamp.Models;
using RVfamcamp.Services;
using System;

public class DetailsModel(DatabaseStatements db) : PageModel
{
    public required Reservation Reservation { get; set; }
    public List<Lot>? lots { get; set; }
    public List<LotType>? lotTypes { get; set; }
    public int ReservationId { get; set; }

    public void OnGet(int id)
    {
        //ReservationId = id;

        //// Get reservation information
        //Reservation = db.GetReservationById(id);

        //// Get all lots associated with reservation
        ////lots = db.GetLotsByReservationId(id);

        //// Get lot types associated with lots
        //lotTypes = (List<LotType>?)db.GetAllLotTypes();

        // Get all lots associated with res and update isoccupied
        List<Lot> lots = new List<Lot>();
        //lots = db.GetLotsByReservationId(id); HOTFIX just reseting the lots avialibilty
        lots = db.getLots();
        foreach (var lot in lots)
        {
            db.UpdateLotOccupancy(lot.LotId, false);
        }



    }
}