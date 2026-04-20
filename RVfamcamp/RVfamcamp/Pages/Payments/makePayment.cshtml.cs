using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RVfamcamp.Configuration;
using RVfamcamp.Models;
using RVfamcamp.Services;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace RVfamcamp.Pages.Payments
{
	public class PaymentLotLine
	{
		public int lotId { get; set; }
		public string typeName { get; set; } = "";
		public decimal nightlyRate { get; set; }
	}

	public class MakePaymentModel : PageModel
	{
		private readonly StripeService _stripe;
		private readonly DatabaseStatements db;
		private readonly PricingSettings _pricingSettings;

		public MakePaymentModel(StripeService stripe, DatabaseStatements db, IOptions<PricingSettings> pricingSettings)
		{
			_stripe = stripe;
			this.db = db;
			_pricingSettings = pricingSettings.Value;
		}

		[BindProperty(SupportsGet = true)]
		public int resID { get; set; }

		public Reservation? reservation { get; set; }

		public List<PaymentLotLine> lotLines { get; set; } = new();

		public int numberOfNights { get; set; }

		public decimal subtotal { get; set; }

		public decimal tax { get; set; }

		public decimal total { get; set; }

		public string errorMessage { get; set; } = "";

		public IActionResult OnGet()
		{
			string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

			if (!int.TryParse(userIdString, out int userId))
			{
				return Redirect("/Login/");
			}

			if (resID <= 0)
			{
				return RedirectToPage("/Index");
			}

			var user = db.GetUserById(userId);

			if (user == null)
			{
				return Redirect("/Login/");
			}

			if (user.Role != "Admin")
			{
				if (!userOwnsReservation(userId, resID))
				{
					return Forbid();
				}
			}

			if (!loadPaymentData(resID))
			{
				return RedirectToPage("/Index");
			}

			return Page();
		}

		public async Task<IActionResult> OnPost()
		{
			string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

			if (!int.TryParse(userIdString, out int userId))
			{
				return Redirect("/Login/");
			}

			if (resID <= 0)
			{
				return RedirectToPage("/Index");
			}

			if (!userOwnsReservation(userId, resID))
			{
				return Forbid();
			}

			if (!loadPaymentData(resID))
			{
				return RedirectToPage("/Index");
			}

			if (db.IsReservationPaid(resID))
			{
				errorMessage = "This reservation has already been paid.";
				return Page();
			}

			var checkoutLineItems = new List<CheckoutLineItemModel>();

			foreach (var lot in lotLines)
			{
				checkoutLineItems.Add(new CheckoutLineItemModel
				{
					name = $"{lot.typeName} - Lot {lot.lotId}",
					unitAmount = lot.nightlyRate,
					quantity = numberOfNights
				});
			}

			if (tax > 0)
			{
				checkoutLineItems.Add(new CheckoutLineItemModel
				{
					name = "Tax",
					unitAmount = tax,
					quantity = 1
				});
			}

			var url = await _stripe.CreateCheckoutSession(checkoutLineItems, userId, resID);
			return Redirect(url);
		}

		private bool loadPaymentData(int reservationId)
		{
			reservation = db.GetReservationById(reservationId);

			if (reservation == null)
			{
				errorMessage = "Reservation not found.";
				return false;
			}

			numberOfNights = (reservation.endDate.Date - reservation.startDate.Date).Days;

			if (numberOfNights < 1)
			{
				numberOfNights = 1;
			}

			var lots = db.GetLotsByReservationId(reservationId);
			var allLotTypes = db.GetAllLotTypes();

			lotLines.Clear();
			subtotal = 0m;
			tax = 0m;

			foreach (var lot in lots)
			{
				var matchingLotType = allLotTypes.FirstOrDefault(lt => lt.LotTypeID == lot.LotType);

				decimal nightlyRate = matchingLotType?.BasePrice ?? 0m;
				string typeName = matchingLotType?.TypeName ?? $"Lot Type {lot.LotType}";

				lotLines.Add(new PaymentLotLine
				{
					lotId = lot.LotId,
					typeName = typeName,
					nightlyRate = nightlyRate
				});

				subtotal += nightlyRate * numberOfNights;
			}

			tax = Math.Round(subtotal * _pricingSettings.DefaultTaxRate, 2);
			total = subtotal + tax;
			return true;
		}

		private bool userOwnsReservation(int userId, int reservationId)
		{
			var usersReservations = db.GetUsersReservations(userId);
			return usersReservations.Any(r => r.reservationId == reservationId);
		}
	}
}