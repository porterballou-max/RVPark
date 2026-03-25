using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using RVfamcamp.Models;
using System.ComponentModel;
using RVfamcamp.Services;

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

        Available_Lots = (List<Lot>?)db.GetVacantLotsOverRange(StartDate, EndDate);
        Available_Lots_Types = (List<LotType>?)db.GetAllLotTypes();

        return Page();
    }

    public IActionResult OnPostCreateReservation()
    {
        if (SelectedLotIds == null) 
        {
            return Page();
        }
        // Split Selected lots into list
        var lotIds = SelectedLotIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .ToList();

        // Create new reservation using StartDate, EndDate, SelectedLotId


        return RedirectToPage("Index");
    }


}