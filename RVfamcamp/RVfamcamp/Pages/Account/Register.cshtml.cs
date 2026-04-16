using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RVfamcamp.Models;
using RVPark.Models;
using RVfamcamp.Services; 

namespace RVfamcamp.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly DatabaseStatements _db;

        public RegisterModel(DatabaseStatements db)
        {
            _db = db;
        }

        [BindProperty]
        public RegisterViewModel Input { get; set; } = new();

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {   
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Pre-check for duplicate email
            if (_db.EmailExists(Input.Email))
            {
                ModelState.AddModelError(string.Empty, "An account with this email already exists.");
                return Page();
            }

            try
            {
                _db.RegisterUser(
                    email: Input.Email,
                    password: Input.Password,
                    firstName: Input.FirstName,
                    lastName: Input.LastName,
                    role: "Client"
                );

                TempData["Success"] = "Account created successfully!";
                return RedirectToPage("/Login");
            }
            catch (SqlException ex)
            {
                // 🛑 Safety net in case duplicate still slips through (race condition)
                if (ex.Message.Contains("AK_EmailAddress") || ex.Message.Contains("duplicate"))
                {
                    ModelState.AddModelError(string.Empty, "That email is already registered.");
                    return Page();
                }

                throw; // unknown errors still surface for debugging
            }
        }
    }
}