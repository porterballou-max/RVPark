using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RVfamcamp.Services;
using RVPark.Models;
using System.Security.Claims;

namespace RVPark.Pages.Profile
{
    [Authorize]
    public class VehicleModel : PageModel
    {
        private readonly DatabaseStatements _db;

        public List<VehicleViewModel> Vehicles { get; set; } = new();

        [BindProperty]
        public VehicleViewModel NewVehicle { get; set; } = new();

        public bool CanAddMoreVehicles { get; set; }
        public bool ShowAddForm { get; set; }

        public VehicleModel(DatabaseStatements db)
        {
            _db = db;
        }

        public void OnGet()
        {
            LoadVehicles();
        }

        public IActionResult OnPostAdd()
        {
            if (!ModelState.IsValid)
            {
                LoadVehicles();
                ShowAddForm = true;
                return Page();
            }

            var userId = GetCurrentUserId();
            if (userId == null)
                return RedirectToPage("/Login");

            // Ensure the user has a Client record (required by FK)
            _db.EnsureClientRecordExists(userId.Value);

            // Now safely add the vehicle
            _db.AddVehicle(
                NewVehicle.LicensePlate ?? "",
                NewVehicle.Year ?? DateTime.Now.Year,
                NewVehicle.Make ?? "",
                NewVehicle.Model ?? "",
                userId.Value);

            TempData["Success"] = "Vehicle added successfully!";
            return RedirectToPage();
        }

        public IActionResult OnPostDelete(int vehicleId)
        {
            if (vehicleId > 0)
            {
                _db.DeleteVehicle(vehicleId);
                TempData["Success"] = "Vehicle removed successfully.";
            }
            return RedirectToPage();
        }

        private void LoadVehicles()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return;
            }

            // Pass .Value since we've already checked it's not null
            Vehicles = _db.GetVehiclesByUser(userId.Value);
            CanAddMoreVehicles = Vehicles.Count < 10;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : null;
        }
    }
}