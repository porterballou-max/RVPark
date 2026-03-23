using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RVfamcamp.Configuration;
using RVfamcamp.Database;
using RVfamcamp.Models;
using RVfamcamp.Services;
using Stripe;
using Stripe.Checkout;
using Microsoft.Extensions.Options;
using System.Text;

namespace RVfamcamp.Controllers
{
	[ApiController]
	[Route("api/stripe")]
	public class StripeWebHookController : ControllerBase
	{
		private readonly PaymentRepo _payments;
		private readonly StripeSettings _config;

		public StripeWebHookController(PaymentRepo payments, IOptions<StripeSettings> config)
		{
			_payments = payments;
			_config = config.Value;
		}

		[HttpPost("webhook")]
		public async Task<IActionResult> Webhook()
		{
			Console.WriteLine("Received Webhook!");

			string json;

			using (var reader = new StreamReader(Request.Body))
			{
				json = await reader.ReadToEndAsync();
			}

			var stripeSignature = Request.Headers["Stripe-Signature"];
			var webhookSecret = _config.WebhookSecret;

			Event stripeEvent;

			try
			{
				stripeEvent = EventUtility.ConstructEvent(
					json,
					stripeSignature,
					webhookSecret
				);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Signature verification failed: {ex.Message}");
				return BadRequest("Invalid Stripe signature.");
			}

			if (stripeEvent.Type == "checkout.session.completed")
			{
				var session = stripeEvent.Data.Object as Session;

				if (session == null)
				{
					Console.WriteLine("Stripe event data was not a Checkout Session.");
					return BadRequest("Invalid session payload.");
				}

				Console.WriteLine($"Stripe Session Id: {session.Id}");
				Console.WriteLine($"Payment Intent Id: {session.PaymentIntentId}");
				Console.WriteLine($"Payment Status: {session.PaymentStatus}");
				Console.WriteLine($"Client Reference Id: {session.ClientReferenceId}");

				paymentModel toAdd = new paymentModel();

				// If your DB column/property is decimal dollars, change this line to:
				// toAdd.total = (session.AmountTotal ?? 0) / 100m;
				toAdd.total = (session.AmountTotal ?? 0) / 100m;

				toAdd.summary = GeneratePaymentSummary(session);
				toAdd.paymentDate = session.Created;
				toAdd.stripeID = session.Id;

				if (_payments.addPayment(toAdd))
				{
					Console.WriteLine("Payment added to DB!");
					return Ok();
				}
				else
				{
					Console.WriteLine("Failed to add to DB!");
					return StatusCode(500, "Failed to save payment to database.");
				}
			}

			Console.WriteLine($"Ignoring unhandled Stripe event type: {stripeEvent.Type}");
			return Ok();
		}

		private static string GeneratePaymentSummary(Session session)
		{
			var summary = new StringBuilder();

			long amountCents = session.AmountTotal ?? 0;
			decimal amountDollars = amountCents / 100m;

			summary.Append("Stripe checkout completed");

			if (!string.IsNullOrWhiteSpace(session.PaymentStatus))
			{
				summary.Append($" | Status: {session.PaymentStatus}");
			}

			summary.Append($" | Amount: {amountDollars:0.00}");

			if (!string.IsNullOrWhiteSpace(session.Currency))
			{
				summary.Append($" {session.Currency.ToUpper()}");
			}

			if (!string.IsNullOrWhiteSpace(session.Id))
			{
				summary.Append($" | Session: {session.Id}");
			}

			if (!string.IsNullOrWhiteSpace(session.PaymentIntentId))
			{
				summary.Append($" | PaymentIntent: {session.PaymentIntentId}");
			}

			if (!string.IsNullOrWhiteSpace(session.ClientReferenceId))
			{
				summary.Append($" | Ref: {session.ClientReferenceId}");
			}

			if (session.CustomerDetails != null && !string.IsNullOrWhiteSpace(session.CustomerDetails.Email))
			{
				summary.Append($" | Email: {session.CustomerDetails.Email}");
			}

			if (session.Metadata != null && session.Metadata.Count > 0)
			{
				foreach (var pair in session.Metadata)
				{
					summary.Append($" | {pair.Key}: {pair.Value}");
				}
			}

			return summary.ToString();
		}
	}
}