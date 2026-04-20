using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RVfamcamp.Models;
using RVfamcamp.Services;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Claims;

public class ReservationsIndexModel(DatabaseStatements db) : PageModel
{
    public List<Reservation> Reservations { get; set; }
    public string? Message { get; set; }
    public bool shouldShowEditButton { get; set; }

    public void OnGet()
    {
        string? role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == "Admin")
        {
            shouldShowEditButton = true;
            Reservations = new List<Reservation>();
            Reservations = db.GetAllReservations();
            
            // for payments
            foreach (var reservation in Reservations)
            {
                reservation.isPayed = db.IsReservationPaid(reservation.reservationId);
            }
        }
        else
        {
            shouldShowEditButton = false;
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
                    foreach (var reservation in Reservations)
                        reservation.isPayed = db.IsReservationPaid(reservation.reservationId);
                }
            }
            else
            {
                Message = "Please click 'Create Reservation' to make a reservation.";
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

    public bool isPayed { get; set; }

}

// 
