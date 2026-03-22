using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace RVfamcamp.Pages
{
    public class LoginModel : PageModel
    {
        public void OnGet()
        {

        }
        // Login information
        public class LoginInput
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        // Input binded in the cshtml
        [BindProperty]
        public LoginInput Input { get; set; }

        // OnPost of form save the information in viewData 
        public void OnPost()
        {
            ViewData["Email"] = Input.Email;
            ViewData["Password"] = Input.Password;
        }

    }
}
