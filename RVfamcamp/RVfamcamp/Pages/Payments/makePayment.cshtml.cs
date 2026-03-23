using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RVfamcamp.Services;

namespace RVfamcamp.Pages.Payments
{
    public class MakePaymentModel : PageModel
	{

		private readonly StripeService _stripe;

		public void OnGet()
        {
        }

		public MakePaymentModel(StripeService stripe)
		{
			_stripe = stripe;
		}

		public async Task<IActionResult> OnPost()
		{
			var url = await _stripe.CreateCheckoutSession(10m, null);

			return Redirect(url);
		}
	}
}
