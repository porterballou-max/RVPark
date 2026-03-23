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