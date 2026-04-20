using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using RVfamcamp.Configuration;
using RVfamcamp.Models;
using RVfamcamp.Services;
using System.Security.Claims;

namespace RVfamcamp.Pages.Payments
{
	public class EditModel : PageModel
	{
		private readonly StripeService _stripe;
		private readonly DatabaseStatements db;
		private readonly PricingSettings _pricingSettings;

		private enum loadState
		{
			success,
			notFound,
			needsOriginalPayment,
			noAdjustmentNeeded
		}

		public EditModel(
			StripeService stripe,
			DatabaseStatements db,
			IOptions<PricingSettings> pricingSettings)
		{
			_stripe = stripe;
			this.db = db;
			_pricingSettings = pricingSettings.Value;
		}

		[BindProperty(SupportsGet = true)]
		public int resID { get; set; }

		public Reservation? reservation { get; set; }
		public paymentModel? payment { get; set; }

		public List<PaymentLotLine> lotLines { get; set; } = new();

		public int numberOfNights { get; set; }

		// revised reservation totals
		public decimal subtotal { get; set; }
		public decimal tax { get; set; }
		public decimal revisedTotal { get; set; }

		// original payment totals
		public decimal originalSubtotal { get; set; }
		public decimal originalTax { get; set; }
		public decimal originalTotal { get; set; }

		// deltas
		public decimal deltaSubtotal { get; set; }
		public decimal deltaTax { get; set; }
		public decimal deltaTotal { get; set; }

		// keep this for the Razor page:
		// absolute amount to charge or refund
		public decimal total { get; set; }

		public bool isRefund { get; set; }

		public string errorMessage { get; set; } = "";

		public IActionResult OnGet()
		{
			var validationResult = validateUserAndReservation(out int userId);
			if (validationResult != null)
			{
				return validationResult;
			}

			var result = loadPaymentData(resID);

			switch (result)
			{
				case loadState.notFound:
					return RedirectToPage("/Index");

				case loadState.needsOriginalPayment:
					return RedirectToPage("/Payments/makePayment", new { resID });

				case loadState.noAdjustmentNeeded:
					errorMessage = "There is no difference in the price of the reservation. No payment or refund is needed.";
					return Page();

				default:
					return Page();
			}
		}

		public async Task<IActionResult> OnPost()
		{
			var validationResult = validateUserAndReservation(out int userId);
			if (validationResult != null)
			{
				return validationResult;
			}

			var result = loadPaymentData(resID);

			switch (result)
			{
				case loadState.notFound:
					return RedirectToPage("/Index");

				case loadState.needsOriginalPayment:
					return RedirectToPage("/Payments/makePayment", new { resID });

				case loadState.noAdjustmentNeeded:
					errorMessage = "There is no difference in the price of the reservation. No payment or refund is needed.";
					return Page();
			}

			if (reservation == null || payment == null)
			{
				errorMessage = "Unable to load payment information.";
				return Page();
			}

			if (!db.IsReservationPaid(resID))
			{
				errorMessage = "This reservation has not yet been paid.";
				return Page();
			}

			if (total <= 0m)
			{
				errorMessage = "There is no adjustment to process.";
				return Page();
			}

			if (isRefund)
			{
				// Partial refund from the original checkout session.
				// Your StripeService already supports an optional amount.
				await _stripe.RefundFromCheckoutSessionAsync(payment.stripeID, total);
				return RedirectToPage("Payments/Confirmation", new { session_id = payment.stripeID });
			}
			else
			{
				// Charge only the difference, not the full reservation again.
				var adjustmentLineItems = buildAdjustmentLineItems();

				string checkoutUrl = await _stripe.CreateCheckoutSession(
					adjustmentLineItems,
					userId,
					resID
				);

				return Redirect(checkoutUrl);
			}
		}

		private IActionResult? validateUserAndReservation(out int userId)
		{
			userId = 0;

			string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

			if (!int.TryParse(userIdString, out userId))
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

			return null;
		}

		private loadState loadPaymentData(int reservationId)
		{
			reservation = db.GetReservationById(reservationId);

			if (reservation == null)
			{
				errorMessage = "Reservation not found.";
				return loadState.notFound;
			}

			int originalPaymentId = db.getPaymentIdByReservation(reservation.reservationId);
			payment = db.getPayment(originalPaymentId);

			if (payment == null)
			{
				errorMessage = "Reservation has not been paid yet.";
				return loadState.needsOriginalPayment;
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

			subtotal = Math.Round(subtotal, 2);
			tax = Math.Round(subtotal * _pricingSettings.DefaultTaxRate, 2);
			revisedTotal = Math.Round(subtotal + tax, 2);

			originalTotal = Math.Round(payment.total, 2);
			originalTax = Math.Round(payment.tax, 2);
			originalSubtotal = Math.Round(originalTotal - originalTax, 2);

			deltaSubtotal = Math.Round(subtotal - originalSubtotal, 2);
			deltaTax = Math.Round(tax - originalTax, 2);
			deltaTotal = Math.Round(revisedTotal - originalTotal, 2);

			total = Math.Abs(deltaTotal);
			isRefund = deltaTotal < 0m;

			if (total == 0m)
			{
				return loadState.noAdjustmentNeeded;
			}

			return loadState.success;
		}

		private List<CheckoutLineItemModel> buildAdjustmentLineItems()
		{
			var items = new List<CheckoutLineItemModel>();

			// Since paymentModel stores original tax, we can split the charge cleanly
			// into subtotal adjustment + tax adjustment, without tracking changed lots.

			if (deltaSubtotal > 0m)
			{
				items.Add(new CheckoutLineItemModel
				{
					name = $"Reservation edit subtotal adjustment #{resID}",
					unitAmount = Math.Round(deltaSubtotal, 2),
					quantity = 1
				});
			}

			if (deltaTax > 0m)
			{
				items.Add(new CheckoutLineItemModel
				{
					name = "Tax adjustment",
					unitAmount = Math.Round(deltaTax, 2),
					quantity = 1
				});
			}

			// Safety fallback in case rounding ever causes both deltas to look zero
			// while total is still positive.
			if (items.Count == 0 && total > 0m)
			{
				items.Add(new CheckoutLineItemModel
				{
					name = $"Reservation edit adjustment #{resID}",
					unitAmount = Math.Round(total, 2),
					quantity = 1
				});
			}

			return items;
		}

		private bool userOwnsReservation(int userId, int reservationId)
		{
			var usersReservations = db.GetUsersReservations(userId);
			return usersReservations.Any(r => r.reservationId == reservationId);
		}
	}
}