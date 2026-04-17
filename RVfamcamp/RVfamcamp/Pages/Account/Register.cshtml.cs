using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using RVfamcamp.Models;
using RVPark.Models;
using RVfamcamp.Services; 

namespace RVfamcamp.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly DatabaseStatements _db;
        private readonly EmailService _email;

        public RegisterModel(DatabaseStatements db, EmailService email)
        {
            _db = db;
            _email = email;
        }

        [BindProperty]
        public RegisterViewModel Input { get; set; } = new();
        
        public void OnGet()
        {
            
        }

        public async Task<ActionResult> OnPost()
        {   
            if (!ModelState.IsValid)
            {
                return Page();
            }

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

                // Send welcome email
                await _email.SendEmail(
                    Input.Email,
                    "Welcome to RV Fam Camp",
                    $@"
                        <div style='font-family: Arial; padding:20px;'>
                            <h2 style='color:#2c3e50;'>Welcome, {Input.FirstName}!</h2>
                            <p>Your account has been successfully created.</p>
                            <p>You can now log in and start making reservations.</p>
                            <p>See our app live at: <a href='https://cs3750-dusklabs-rvfamcamp-hba9fkevc6h2a4e4.centralus-01.azurewebsites.net/' target='_blank'>RVFamcamp</a></p>
                            <hr />
                            <small>Cheers, from Dusk Labs - CS 3750 - Spring Semester 2026.</small>
                        </div>
                    "
                );

                TempData["Success"] = "Account created successfully!";
                return RedirectToPage("/Login");
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601 || ex.Number == 2627)
                {
                    ModelState.AddModelError(string.Empty, "That email is already registered.");
                    return Page();
                }

                throw;
            }
        }
    }
}