using RVfamcamp.Configuration;
using RVfamcamp.Models;
using Microsoft.Extensions.Options;
using Stripe.Checkout;
using Stripe;
using RVfamcamp.Pages.Payments;

namespace RVfamcamp.Services
{
	public class StripeService
	{
		private readonly AppSettings _settings;
		private readonly PricingSettings _pricingSettings;
		private readonly DatabaseStatements _db;

		public StripeService(IOptions<AppSettings> options, IOptions<PricingSettings> pricingSettings, DatabaseStatements databaseStatements)
		{
			_settings = options.Value;
			_pricingSettings = pricingSettings.Value;
			_db = databaseStatements;
		}

		public async Task<string> CreateCheckoutSession(
			List<CheckoutLineItemModel> lineItems,
			int? userID,
			int resID)
		{
			int _userID = 1;
			if (userID.HasValue)
			{
				_userID = userID.Value;
			}
		
			var curUser = _db.GetUserById(_userID);

			var stripeLineItems = new List<SessionLineItemOptions>();

			foreach (var item in lineItems)
			{
				stripeLineItems.Add(new SessionLineItemOptions
				{
					Quantity = item.quantity,
					PriceData = new SessionLineItemPriceDataOptions
					{
						Currency = _pricingSettings.Currency,
						UnitAmount = (long)(item.unitAmount * 100),
						ProductData = new SessionLineItemPriceDataProductDataOptions
						{
							Name = item.name
						}
					}
				});
			}

			var options = new SessionCreateOptions
			{
				Mode = "payment",
				SuccessUrl = _settings.BaseUrl + "/Payments/Confirmation?session_id={CHECKOUT_SESSION_ID}",
				CancelUrl = _settings.BaseUrl + "/Payments/Cancelation?resId=" + resID,
				ClientReferenceId = _userID.ToString(),
				CustomerEmail = curUser.Email,
				PaymentIntentData = new SessionPaymentIntentDataOptions
				{
					ReceiptEmail = curUser.Email
				},
				Metadata = new Dictionary<string, string>
	{
		{ "userId", _userID.ToString() },
		{ "reservationId", resID.ToString() }
	},
				LineItems = stripeLineItems
			};

			var service = new SessionService();
			var session = await service.CreateAsync(options);

			return session.Url;
		}

		public async Task<Refund> RefundFromCheckoutSessionAsync(string checkoutSessionId, decimal? amountDollars = null)
		{
			var sessionService = new SessionService();
			var session = await sessionService.GetAsync(checkoutSessionId);

			if (string.IsNullOrWhiteSpace(session.PaymentIntentId))
			{
				throw new InvalidOperationException("This Checkout Session does not have a PaymentIntent.");
			}

			var refundOptions = new RefundCreateOptions
			{
				PaymentIntent = session.PaymentIntentId,
				Reason = "requested_by_customer"
			};

			// Leave Amount unset for full refund
			if (amountDollars.HasValue)
			{
				refundOptions.Amount = (long)(amountDollars.Value * 100m);
			}

			var refundService = new RefundService();
			return await refundService.CreateAsync(refundOptions);
		}
	}
}