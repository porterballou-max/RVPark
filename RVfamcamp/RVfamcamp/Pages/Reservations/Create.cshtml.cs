using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using RVfamcamp.Models;
using System.ComponentModel;

public class CreateModel : PageModel
{
    public required Reservation Reservation { get; set; }

    [BindProperty]
    public DateTime StartDate { get; set; }

    [BindProperty]
    public DateTime EndDate { get; set; }

    public void OnGet()
    {
        Reservation = new Reservation();
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
            return Page();

        // TODO: Save to database

        return RedirectToPage("Index");
    }
}