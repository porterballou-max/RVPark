using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RVfamcamp.Models;
using RVfamcamp.Services;

namespace RVfamcamp.Pages.Reservations
{
    public class DeleteModel(DatabaseStatements db) : PageModel
    {
        public string Message { get; set; }
        public IActionResult OnGet(int id)
        {
            
            var payment = db.getPaymentIdByReservation(id);
            if (payment != 0)
            {
                TempData["Message"] = "Reservation cannot be deleted payment already made";
			}
			else
            {
                // Get all lots associated with res and remove
                db.ClearLotsFromReservation(id);
                db.RemoveReservationById(id);

                return RedirectToPage("Index");
            }

        }
    }
}
