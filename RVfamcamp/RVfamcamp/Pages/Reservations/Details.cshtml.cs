using Microsoft.AspNetCore.Mvc.RazorPages;
using RVfamcamp.Models;
using System;

public class DetailsModel : PageModel
{
    public Reservation Reservation { get; set; }

    public void OnGet(int id)
    {
        // TODO: Replace with DB lookup
        Reservation = new Reservation { };
    }
}