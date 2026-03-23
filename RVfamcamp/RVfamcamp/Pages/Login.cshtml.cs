using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using RVfamcamp.Services;
using RVfamcamp.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;



namespace RVfamcamp.Pages
{
    public class LoginModel : PageModel
    {
        private readonly DatabaseStatements _db;

        public LoginModel(DatabaseStatements db)
        {
            _db = db;
        }


        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = "";

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = "";

            [Display(Name = "")]
            public bool RememberMe { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return Page();
            }
            
            var user = _db.LoginUserAccount(Input.Email, Input.Password);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserAccountId.ToString()),
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("FullName", $"{user.FirstName} {user.LastName}"),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = Input.RememberMe,
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProperties);

                return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return Page();
        }
       
        // Simple DTO for the logged-in user data we need
        private class UserAccount
        {
            public int UserAccountID { get; set; }
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public string EmailAddress { get; set; } = "";
            public string Username { get; set; } = "";
            public string Role { get; set; } = "";
        }



    }
}
