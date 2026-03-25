using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RVfamcamp.Models;
using RVfamcamp.Services;
using Stripe;
using System;
using System.ComponentModel;
using System.Security.Claims;
using System.Security.Cryptography;

public class CreateModel(DatabaseStatements db) : PageModel
{
    public required Reservation Reservation { get; set; }
    public List<Lot>? Available_Lots { get; set; }
    public List<LotType>? Available_Lots_Types { get; set; }

    [BindProperty]
    public DateOnly StartDate { get; set; }

    [BindProperty]
    public DateOnly EndDate { get; set; }

    [BindProperty]
    public string? SelectedLotIds { get; set; }

    public void OnGet()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        StartDate = today;
        EndDate = today.AddDays(1);
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

        if (EndDate< StartDate)
        {
            ModelState.AddModelError("", "End date cannot be before start date.");
        }

        if (!ModelState.IsValid)
            return Page();

        Available_Lots = (List<Lot>?)db.GetVacantLots(StartDate, EndDate);
        Available_Lots_Types = (List<LotType>?)db.GetAllLotTypes();

        return Page();
    }

    public IActionResult OnPostCreateReservation()
    {
        if (SelectedLotIds == null) 
        {
            return Page();
        }

        // Get current userID
        string? email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (email == null)
        {
            return RedirectToPage("../Login");
        }
        var userID = db.GetUserAccountID(email);

        // Split Selected lots into list
        var lotIDs = SelectedLotIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .ToList();

        // Generate random confirmation number
        var rand = new Random();
        int number = rand.Next(10000, 100000);

        // initialize reservation
        Reservation = new Reservation
        {
            // Create new reservation using StartDate, EndDate, SelectedLotId
            startDate = StartDate.ToDateTime(TimeOnly.MinValue),
            endDate = EndDate.ToDateTime(TimeOnly.MinValue),
            confirmationNumber = number
        };

        // Add Reservation to database
        int resID = db.AddReservation(Reservation, userID);

        // Tie reservation to selected lots
        foreach (var lotID in lotIDs){
            db.AssignLotToReservation(lotID, resID);

            // Change lot availability
            db.UpdateLotOccupancy(lotID, true);
        }

        return RedirectToPage("Index");
    }


}