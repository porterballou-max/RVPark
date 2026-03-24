using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RVfamcamp.Services;
using System.Security.Claims;

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

			string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

			if (int.TryParse(userIdString, out int userId))
			{
				var url = await _stripe.CreateCheckoutSession(10m, userId);

				return Redirect(url);
			} else
			{
				return Redirect("/Login/");
			}
		}
	}
}
