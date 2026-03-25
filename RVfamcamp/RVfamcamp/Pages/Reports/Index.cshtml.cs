using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RVfamcamp.Models;
using RVfamcamp.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RVfamcamp.Pages
{
    public class ReportsModel : PageModel
    {
        private readonly DatabaseStatements _db;

        public ReportsModel(DatabaseStatements db)
        {
            _db = db;
        }

        [BindProperty]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [BindProperty]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(7);

        public bool ShowReports { get; set; } = false;

        public StatusReport? MyStatusReport { get; set; }

        public void OnGet()
        {

        }

        public void OnPost()
        {
            // Find the user ID
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int loggedInUserID))
            {
                ShowReports = true;
                MyStatusReport = _db.GetStatusReport(StartDate, EndDate);

                string reportTitle = $"Status Report ({StartDate.ToShortDateString()} - {EndDate.ToShortDateString()})";

                _db.LogReportGeneration(loggedInUserID, reportTitle);
            }
            //If the claim for the UserID is missing, assume logged out and return to login screen.
            else
            {
                RedirectToPage("/Login");
            }
        }
    }
}