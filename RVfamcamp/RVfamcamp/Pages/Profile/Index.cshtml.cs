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
    public class IndexModel : PageModel
    {
        private readonly DatabaseStatements _db;
        private readonly EmailService _email;

        public ProfileViewModel Profile { get; set; } = new();

        public IndexModel(DatabaseStatements db, EmailService email)
        {
            _db = db;
            _email = email;
        }

        public void OnGet()
        {
            LoadProfile();
        }
        
        public async Task<IActionResult> OnPostConfirmEmail()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToPage();

            var user = _db.GetUserById(userId);
            if (user == null)
                return RedirectToPage();

            // Allows for sending test/confirmation email
            await _email.SendEmail(
                user.Email,
                "Email Confirmation - RV Fam Camp",
                $@"
                    <div style='font-family: Arial; padding:20px;'>
                        <h2>Email Confirmation</h2>
                        <p>Hello {user.FirstName},</p>
                        <p>This email confirms that your email address is valid and working.</p>
                        <p>See our app live at: <a href='https://cs3750-dusklabs-rvfamcamp-hba9fkevc6h2a4e4.centralus-01.azurewebsites.net/' target='_blank'>RVFamcamp</a></p>
                        <hr />
                        <small>Cheers, from Dusk Labs - CS 3750 - Spring Semester 2026.</small>
                    </div>
                "
            );

            TempData["Success"] = "Confirmation email sent successfully!";
            return RedirectToPage(); // reload page to show message
        }

        // 🔁 Reusable loader
        private void LoadProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return;

            var user = _db.GetUserById(userId);
            if (user == null) return;

            Profile = new ProfileViewModel
            {
                Id = user.UserAccountId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
            };
        }
    }
}