using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RVfamcamp.Database;
using RVfamcamp.Models;
using RVfamcamp.Services;

namespace RVfamcamp.Pages.Payments
{
    public class ViewPaymentsModel : PageModel
	{

		private readonly StripeService _stripe;
		private readonly PaymentRepo _paymentRepo;

		public List<paymentModel> payments { get; set; } = new();


		public void OnGet()
        {
			payments = _paymentRepo.getAllPayments(); // TODO: Hook into authorized user to get payments for the logged in user!
		}

		public ViewPaymentsModel(StripeService stripe, PaymentRepo paymentRepo)
		{
			_stripe = stripe;
			_paymentRepo = paymentRepo;
		}
	}
}
