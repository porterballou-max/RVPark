using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RVfamcamp.Models;
using RVfamcamp.Services;
using RVPark.Models;

namespace RVPark.Pages.Profile
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly DatabaseStatements _db;

        public ProfileViewModel Profile { get; set; } = new();

        public IndexModel(DatabaseStatements db)
        {
            _db = db;
        }

        public void OnGet()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return;

            var user = _db.GetUserById(userId);
            if (user == null) return;

            var client = _db.GetClientInfo(userId);

            Profile = new ProfileViewModel
            {
                Id = user.UserAccountId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                // Phone removed - we don't store it
            };
        }
    }
}