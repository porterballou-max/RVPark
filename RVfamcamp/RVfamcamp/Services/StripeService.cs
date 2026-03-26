using RVfamcamp.Configuration;
using RVfamcamp.Models;
using Microsoft.Extensions.Options;
using Stripe.Checkout;
using RVfamcamp.Pages.Payments;

namespace RVfamcamp.Services
{
	public class StripeService
	{
		private readonly AppSettings _settings;
		private readonly PricingSettings _pricingSettings;

		public StripeService(IOptions<AppSettings> options, IOptions<PricingSettings> pricingSettings)
		{
			_settings = options.Value;
			_pricingSettings = pricingSettings.Value;
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
	}
}