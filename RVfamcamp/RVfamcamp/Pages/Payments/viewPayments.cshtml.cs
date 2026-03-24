using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RVfamcamp.Database;
using RVfamcamp.Models;
using RVfamcamp.Services;
using System.Security.Claims;

namespace RVfamcamp.Pages.Payments
{
    public class ViewPaymentsModel : PageModel
	{

		private readonly StripeService _stripe;
		private readonly PaymentRepo _paymentRepo;

		public List<paymentModel> payments { get; set; } = new();

		public IActionResult OnGet()
		{
			string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			Console.WriteLine("user id string: " + userIdString);
			if (!int.TryParse(userIdString, out int userId))
				return Redirect("/Login");

			payments = _paymentRepo.getAllPaymentsForUser(userId);

			return Page();
		}

		public ViewPaymentsModel(StripeService stripe, PaymentRepo paymentRepo)
		{
			_stripe = stripe;
			_paymentRepo = paymentRepo;
		}
	}
}
