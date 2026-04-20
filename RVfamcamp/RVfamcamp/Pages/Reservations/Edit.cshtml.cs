using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using RVfamcamp.Models;
using RVfamcamp.Services;

public class EditModel(DatabaseStatements db) : PageModel
{
    public required Reservation Reservation { get; set; }
    public List<Lot>? Available_Lots { get; set; }
    public required List<Lot>? ReservationLots { get; set; }
    public bool datesAccepted { get; set; }

    [BindProperty]
    public DateOnly StartDate { get; set; }

    [BindProperty]
    public DateOnly EndDate { get; set; }
    [BindProperty]
    public int reservationID { get; set; }

    public void OnGet(int id)
    {
        Reservation = db.GetReservationById(id);
        reservationID = Reservation.reservationId;
        StartDate = DateOnly.FromDateTime(Reservation.startDate);
        EndDate = DateOnly.FromDateTime(Reservation.endDate);
    }
    public IActionResult OnPostCheckAvailability()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (StartDate < today)
        {
            ModelState.AddModelError("", "Start date cannot be in the past.");
        }

        if (EndDate < today)
        {
            ModelState.AddModelError("", "End date cannot be in the past.");
        }

        if (EndDate < StartDate)
        {
            ModelState.AddModelError("", "End date cannot be before start date.");
        }

        if (!ModelState.IsValid)
            return Page();

        ReservationLots = (List<Lot>?)db.GetLotsByReservationId(reservationID);

        // momentarily remove lots from reservation to help with availability check
        db.ClearLotsFromReservation(reservationID);

        // check for lot availability over new range
        Available_Lots = (List<Lot>?)db.GetUnreservedLotsOverRange(StartDate, EndDate);
        datesAccepted = true;
        foreach (Lot lot in ReservationLots)
        {
            if (Available_Lots.Contains(lot))
            {
                datesAccepted = false;
                ModelState.AddModelError("", "Lots is already reserved in that date range.");
                break;

            }
            // Add lots back to reservation
            db.AssignLotToReservation(lot.LotId, reservationID);
        }

        return Page();
    }
    public IActionResult OnPostSaveChanges()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        // TODO: Update database
        else
        {
            db.EditReservation(reservationID, StartDate, EndDate);
        }

        return RedirectToPage("Index");
    }
}