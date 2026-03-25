using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using RVfamcamp.Models;
using RVfamcamp.Services;
using System.Linq.Expressions;
using System.Security.Claims;

public class ReservationsIndexModel(DatabaseStatements db) : PageModel
{
    public List<Reservation> Reservations { get; set; }
    public string? Message { get; set; }

    public void OnGet()
    {
        Reservations = new List<Reservation>();
        string? email = User.FindFirst(ClaimTypes.Email)?.Value;

        // Ensure there is a user logged in
        if (email != null && db != null)
        {
            var userID = db.GetUserAccountID(email);

            // Ensure there is a userAccount tied to username
            if (userID != -1)
            {
                Reservations = db.GetUsersReservations(userID);
            }
        }
        else
        {
            Message = "Please click 'Create Reservation' to make a reservation.";
        }
    } 
}

// Reservations class
public class Reservation
{
    public int reservationId { get; set; }
    public DateTime startDate { get; set; }
    public DateTime endDate { get; set; }
    public int confirmationNumber { get; set; }

}

// 
