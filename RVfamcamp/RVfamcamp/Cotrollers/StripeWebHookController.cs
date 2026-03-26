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
				if(int.TryParse(session.ClientReferenceId, out int userId))
					toAdd.userID = userId;
				toAdd.summary = GeneratePaymentSummary(session);
				toAdd.paymentDate = session.Created;
				toAdd.stripeID = session.Id;
				string? reservationIdString = null;

				if (session.Metadata != null &&
					session.Metadata.TryGetValue("reservationId", out var strResID))
				{
					reservationIdString = strResID;
				}
				if (int.TryParse(reservationIdString, out int reservationId))
				{
					toAdd.reservationID = reservationId;
				}
				else
				{
					// Handle error
				}

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
			long amountCents = session.AmountTotal ?? 0;
			decimal amountDollars = amountCents / 100m;

			var parts = new List<string>();

			// Core info (never removed)
			parts.Add("Stripe checkout");

			if (!string.IsNullOrWhiteSpace(session.PaymentStatus))
				parts.Add($"Status: {session.PaymentStatus}");

			parts.Add($"Amount: {amountDollars:0.00} {(session.Currency ?? "usd").ToUpper()}");

			if (!string.IsNullOrWhiteSpace(session.Id))
				parts.Add($"Session: {session.Id}");

			if (!string.IsNullOrWhiteSpace(session.PaymentIntentId))
				parts.Add($"PI: {session.PaymentIntentId}");

			// Secondary info (only include if space allows)
			if (!string.IsNullOrWhiteSpace(session.ClientReferenceId))
				parts.Add($"Ref: {session.ClientReferenceId}");

			if (session.CustomerDetails != null &&
				!string.IsNullOrWhiteSpace(session.CustomerDetails.Email))
			{
				parts.Add($"Email: {session.CustomerDetails.Email}");
			}

			// Build safely within 255
			return JoinWithLimit(parts, 255);
		}

		private static string JoinWithLimit(List<string> parts, int maxLength)
		{
			var result = parts[0]; // always include first

			for (int i = 1; i < parts.Count; i++)
			{
				string next = result + " | " + parts[i];

				if (next.Length > maxLength)
					break;

				result = next;
			}

			return result;
		}
	}
}