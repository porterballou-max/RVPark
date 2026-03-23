using RVfamcamp.Configuration;
using RVfamcamp.Models;
using Microsoft.Extensions.Options;
using Stripe.Checkout;

namespace RVfamcamp.Services
{
	public class StripeService
	{
		private readonly AppSettings _settings;

		public StripeService(IOptions<AppSettings> options)
		{
			_settings = options.Value;
		}

		public async Task<string> CreateCheckoutSession(decimal amount, int? userID)
		{
			int _userID = 1;
			if (userID.HasValue)
			{
				_userID = userID.Value;
			}
			var options = new Stripe.Checkout.SessionCreateOptions
			{
				Mode = "payment",

				SuccessUrl = _settings.BaseUrl + "/Payments/Confirmation?session_id={CHECKOUT_SESSION_ID}",
				CancelUrl = _settings.BaseUrl + "/Payments/Cancelation",

				ClientReferenceId = _userID.ToString(),

				Metadata = new Dictionary<string, string>
				{
					{ "userId", _userID.ToString() },
				},

				LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
					{
						new Stripe.Checkout.SessionLineItemOptions
						{
							Quantity = 1,
							PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
							{
								Currency = "usd",
								UnitAmount = (long)(amount * 100),
								ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
							{
							Name = "RV Park Reservation Payment"
							}
						}
					}
				}
			};

			var service = new SessionService();
			var session = await service.CreateAsync(options);

			return session.Url;
		}
	}
}