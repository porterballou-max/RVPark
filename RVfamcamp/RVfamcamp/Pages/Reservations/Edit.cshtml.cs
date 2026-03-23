using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using RVfamcamp.Models;

public class EditModel : PageModel
{
    [BindProperty]
    public Reservation Reservation { get; set; }

    public void OnGet(int id)
    {
        // TODO: Replace with DB lookup
        Reservation = new Reservation
        {
            ReservationId = id,
            // SiteNumber = "A12",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(3),
            ConfirmationNumber = 0,
            UserAccountId = 0
            // GuestCount = 2,
            // TotalCost = 150,
            // Status = "Confirmed"
        };
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
            return Page();

        // TODO: Update database

        return RedirectToPage("Index");
    }
}