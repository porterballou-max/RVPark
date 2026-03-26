using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RVfamcamp.Pages.Payments
{
    public class CancelationModel : PageModel
    {
        public int reservationId { get; set; }

		public void OnGet(int resId)
        {
            reservationId = resId;
        }
    }
}
