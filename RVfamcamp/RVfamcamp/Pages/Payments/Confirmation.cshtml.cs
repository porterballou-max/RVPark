using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RVfamcamp.Configuration;
using RVfamcamp.Models;
using RVfamcamp.Services;
using Microsoft.Extensions.Options;
using Stripe.Checkout;

namespace RVfamcamp.Pages.Payments
{
	public class ConfirmationModel : PageModel
	{

		private readonly DatabaseStatements _db;
		private readonly PricingSettings _pricingSettings;

		public ConfirmationModel(DatabaseStatements db, IOptions<PricingSettings> pricingSettings)
		{
			_pricingSettings = pricingSettings.Value;
			_db = db;
		}

		public string sessionId { get; set; } = "";
		
		public Reservation reservation { get; set; }
		
		public Session? stripeSession { get; set; }

		public List<PaymentLotLine> lotLines { get; set; } = new();

		public int numberOfNights { get; set; }

		public decimal subtotal { get; set; }

		public decimal tax { get; set; }

		public decimal total { get; set; }

		public async Task<IActionResult> OnGet(string? session_id)
		{
			if (string.IsNullOrWhiteSpace(session_id))
			{
				return RedirectToPage("/Index");
			}

			sessionId = session_id;

			var service = new SessionService();
			stripeSession = await service.GetAsync(session_id);

			string? reservationIdString = null;

			if (stripeSession.Metadata != null &&
			stripeSession.Metadata.TryGetValue("reservationId", out var strResID))
			{
				reservationIdString = strResID;
			}
			if (int.TryParse(reservationIdString, out int reservationId))
			{
				reservation = _db.GetReservationById(reservationId);
			}

			loadPaymentData();

			return Page();
		}

		private bool loadPaymentData()
		{
			if (reservation == null)
			{
				return false;
			}

			numberOfNights = (reservation.endDate.Date - reservation.startDate.Date).Days;

			if (numberOfNights < 1)
			{
				numberOfNights = 1;
			}

			var lots = _db.GetLotsByReservationId(reservation.reservationId);
			var allLotTypes = _db.GetAllLotTypes();

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
	}
}