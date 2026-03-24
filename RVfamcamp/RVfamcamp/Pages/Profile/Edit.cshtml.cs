using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RVfamcamp.Models;
using RVfamcamp.Services;
using RVPark.Models;
using System.Security.Claims;

namespace RVPark.Pages.Profile
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly DatabaseStatements _db;

        [BindProperty]
        public EditProfileViewModel Input { get; set; } = new();

        public bool HasClientRecord { get; set; }

        public EditModel(DatabaseStatements db)
        {
            _db = db;
        }

        public void OnGet()
        {
            LoadCurrentUserData();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToPage("/Login");
            }

            // Check if any changes were actually made
            var currentUser = _db.GetUserById(userId);
            var currentClient = _db.GetClientInfo(userId);

            if (NoChangesMade(currentUser, currentClient))
            {
                TempData["Info"] = "No changes were made.";
                return RedirectToPage("./Index");
            }

            // Save changes
            _db.UpdateUser(userId, Input.Email, Input.FirstName, Input.LastName);

            // Only update Client table if the user actually has a Client record
            if (HasClientRecord)
            {
                _db.EditClientInfo(userId,
                    Input.MilitaryAffiliation ?? "",
                    Input.BillingStreet ?? "",
                    Input.BillingCity ?? "",
                    Input.BillingState ?? "",
                    Input.BillingZip ?? "");
            }

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToPage("./Index");
        }

        private void LoadCurrentUserData()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return;

            var user = _db.GetUserById(userId);
            if (user == null) return;

            var client = _db.GetClientInfo(userId);

            Input = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                MilitaryAffiliation = client?.MilitaryAffiliation ?? "",
                BillingStreet = client?.BillingStreet ?? "",
                BillingCity = client?.BillingCity ?? "",
                BillingState = client?.BillingState ?? "",
                BillingZip = client?.BillingZip ?? ""
            };

            HasClientRecord = client != null;
        }

        private bool NoChangesMade(UserAccount? currentUser, ClientInfo? currentClient)
        {
            if (currentUser == null) return false;

            return currentUser.FirstName == Input.FirstName &&
                   currentUser.LastName == Input.LastName &&
                   currentUser.Email == Input.Email &&
                   (currentClient?.MilitaryAffiliation ?? "") == Input.MilitaryAffiliation &&
                   (currentClient?.BillingStreet ?? "") == Input.BillingStreet &&
                   (currentClient?.BillingCity ?? "") == Input.BillingCity &&
                   (currentClient?.BillingState ?? "") == Input.BillingState &&
                   (currentClient?.BillingZip ?? "") == Input.BillingZip;
        }
    }
}