using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using RVfamcamp.Models;
using RVfamcamp.Services;
using System.Linq.Expressions;
using System.Security.Claims;

public class ReservationsIndexModel : PageModel
{
    public List<Reservation> Reservations { get; set; }
    private DatabaseStatements statements;

    public void OnGet()
    {
        Reservations = new List<Reservation>();

        // Ensure there is a user logged in
        if (User.Identity.Name != null)
        {
            var userID = statements.GetUserID(User.Identity.Name);

            // Ensure there is a userAccount tied to username
            if (userID != -1)
            {
                Reservations = statements.GetUsersReservations(userID);
            }
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