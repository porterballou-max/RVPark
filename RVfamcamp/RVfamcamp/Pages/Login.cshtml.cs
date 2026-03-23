using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.Data.SqlClient;


namespace RVfamcamp.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public LoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
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

            var user = await AuthenticateUserAsync(Input.Email, Input.Password);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserAccountID.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.EmailAddress),
                    new Claim("FullName", $"{user.FirstName} {user.LastName}"),
                    new Claim(ClaimTypes.Role, user.Role)
                    // Can add more claims later, e.g. militaryAffiliation from Client table if needed
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

        private async Task<UserAccount?> AuthenticateUserAsync(string email, string password)
        {
            string connectionString = _configuration.GetConnectionString("MyDbConnection")
                ?? throw new InvalidOperationException("Connection string 'MyDbConnection' not found.");

            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();


                using var command = new SqlCommand(
                    @"SELECT userAccountID, firstName, lastName, emailAddress, username, role
                  FROM UserAccount 
                  WHERE emailAddress = @Email AND password = @Password",
                    connection);

                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@Password", password);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new UserAccount
                    {
                        UserAccountID = reader.GetInt32(0),
                        FirstName = reader.GetString(1),
                        LastName = reader.GetString(2),
                        EmailAddress = reader.GetString(3),
                        Username = reader.GetString(4),
                        Role = reader.GetString(5)
                    };
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Error: {ex.Number} - {ex.Message}");
                return null;
            }

            return null;
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
