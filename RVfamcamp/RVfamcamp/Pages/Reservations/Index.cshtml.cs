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
        string? email = User.FindFirst(ClaimTypes.Email)?.Value;

        // Ensure there is a user logged in
        if (email != null)
        {
            var userID = statements.GetUserAccountID(email);

            // Ensure there is a userAccount tied to username
            if (userID != -1)
            {
                Reservations = statements.GetUsersReservations(userID);
            }
        }
        else
        {
            Reservations.Add
            (
                new Reservation
                {
                    reservationId = 1,
                    startDate = DateTime.Today,
                    endDate = DateTime.Today.AddDays(3),
                    confirmationNumber = 101
                }
            );
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