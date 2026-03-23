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

            // Call your DB service to register the user
            _db.RegisterUser(
                email: Input.Email,
                password: Input.Password,
                firstName: Input.FirstName,
                lastName: Input.LastName,
                role: "Client" // default role
            );

            TempData["Success"] = "Account created successfully!";

            return RedirectToPage("/Login");
        }
    }
}