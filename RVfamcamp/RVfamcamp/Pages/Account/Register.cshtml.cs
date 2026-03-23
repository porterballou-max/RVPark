using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RVPark.Models;

namespace RVPark.Pages.Account
{
    public class RegisterModel : PageModel
    {
        [BindProperty]
        public RegisterViewModel Input { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // TODO: Save user to database
            // - Hash password
            // - Store user record

            TempData["Success"] = "Account created successfully!";

            return RedirectToPage("/Login");
            // OR RedirectToPage("/Profile/Index");
        }
    }
}