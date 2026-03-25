using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using RVfamcamp.Models;
using RVfamcamp.Services;

public class EditModel(DatabaseStatements db) : PageModel
{
    [BindProperty]
    public Reservation Reservation { get; set; }

    public void OnGet(int id)
    {
        Reservation = db.GetReservationById(id);
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        // TODO: Update database
        else
        {
            
        }

        return RedirectToPage("Index");
    }
}