using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using RVfamcamp.Models;

public class ReservationsIndexModel : PageModel
{
    public List<Reservation> Reservations { get; set; }

    public void OnGet()
    {
        // Temporary mock data
        // Reservations = new List<Reservation>
        // {
        //     new Reservation
        //     {
        //         Id = 1,
        //         SiteNumber = "A12",
        //         CheckIn = DateTime.Today,
        //         CheckOut = DateTime.Today.AddDays(3),
        //         GuestCount = 2,
        //         TotalCost = 150.00,
        //         Status = "Confirmed"
        //     },
        //     new Reservation
        //     {
        //         Id = 2,
        //         SiteNumber = "B07",
        //         CheckIn = DateTime.Today.AddDays(5),
        //         CheckOut = DateTime.Today.AddDays(7),
        //         GuestCount = 4,
        //         TotalCost = 220.00,
        //         Status = "Pending"
        //     }
        // };
    }
}

// /* Simple model (replace later with your actual model) */
// public class Reservation
// {
//     public int Id { get; set; }
//     public string SiteNumber { get; set; }
//     public DateTime CheckIn { get; set; }
//     public DateTime CheckOut { get; set; }
//     public int GuestCount { get; set; }
//     public double TotalCost { get; set; }
//     public string Status { get; set; }
// }