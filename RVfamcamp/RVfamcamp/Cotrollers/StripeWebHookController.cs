using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RVfamcamp.Configuration;
using RVfamcamp.Database;
using RVfamcamp.Models;
using Stripe;
using Stripe.Checkout;
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
			Console.WriteLine("Received Stripe webhook.");

			string json = await ReadRequestBodyAsync();
			Event stripeEvent;

			try
			{
				stripeEvent = ConstructVerifiedEvent(json);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Stripe signature verification failed: {ex.Message}");
				return BadRequest("Invalid Stripe signature.");
			}

			try
			{
				await ProcessEventAsync(stripeEvent);
				return Ok();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Webhook processing failed for event {stripeEvent.Id}: {ex}");
				return StatusCode(500, "Webhook processing failed.");
			}
		}

		private async Task<string> ReadRequestBodyAsync()
		{
			using var reader = new StreamReader(Request.Body);
			return await reader.ReadToEndAsync();
		}

		private Event ConstructVerifiedEvent(string json)
		{
			var stripeSignature = Request.Headers["Stripe-Signature"];
			var webhookSecret = _config.WebhookSecret;

			return EventUtility.ConstructEvent(
				json,
				stripeSignature,
				webhookSecret
			);
		}

		private async Task ProcessEventAsync(Event stripeEvent)
		{
			switch (stripeEvent.Type)
			{
				case "checkout.session.completed":
					{
						var session = stripeEvent.Data.Object as Session;
						if (session == null)
							throw new InvalidOperationException("Event data was not a Checkout Session.");

						await HandleCheckoutSessionCompletedAsync(session);
						break;
					}

				case "refund.updated":
					{
						var refund = stripeEvent.Data.Object as Refund;
						if (refund == null)
							throw new InvalidOperationException("Event data was not a Refund.");

						await HandleRefundUpdatedAsync(refund);
						break;
					}

				case "charge.refunded":
					{
						var charge = stripeEvent.Data.Object as Charge;
						if (charge == null)
							throw new InvalidOperationException("Event data was not a Charge.");

						await HandleChargeRefundedAsync(charge);
						break;
					}

				default:
					{
						Console.WriteLine($"Ignoring unhandled Stripe event type: {stripeEvent.Type}");
						break;
					}
			}
		}

		private async Task HandleCheckoutSessionCompletedAsync(Session session)
		{
			Console.WriteLine($"Stripe Session Id: {session.Id}");
			Console.WriteLine($"Payment Intent Id: {session.PaymentIntentId}");
			Console.WriteLine($"Payment Status: {session.PaymentStatus}");
			Console.WriteLine($"Client Reference Id: {session.ClientReferenceId}");

			var toAdd = new paymentModel
			{
				total = (session.AmountTotal ?? 0) / 100m,
				summary = GeneratePaymentSummary(session),
				paymentDate = session.Created,
				stripeID = session.Id
			};

			if (int.TryParse(session.ClientReferenceId, out int userId))
				toAdd.userID = userId;

			if (session.Metadata != null &&
				session.Metadata.TryGetValue("reservationId", out var strResID) &&
				int.TryParse(strResID, out int reservationId))
			{
				toAdd.reservationID = reservationId;
			}
			else
			{
				Console.WriteLine("Reservation ID missing or invalid in session metadata.");
			}

			if (_payments.addPayment(toAdd))
			{
				Console.WriteLine("Payment added to DB.");
			}
			else
			{
				throw new Exception("Failed to save payment to database.");
			}

			await Task.CompletedTask;
		}

		private async Task HandleRefundUpdatedAsync(Refund refund)
		{
			if (!string.Equals(refund.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
				return;

			string? checkoutSessionId = await FindCheckoutSessionIdByPaymentIntentAsync(refund.PaymentIntentId);
			if (string.IsNullOrWhiteSpace(checkoutSessionId))
				return;

			var original = _payments.getPaymentByStripeID(checkoutSessionId);
			if (original == null || original.id == 0)
				return;

			decimal refundAmount = refund.Amount / 100m;

			// Full refund
			decimal refundTax = -original.tax;
			decimal refundTotal = -original.total;

			// If you support partial refunds, prorate instead:
			// decimal ratio = refundAmount / original.total;
			// decimal refundTax = -(original.tax * ratio);
			// decimal refundTotal = -refundAmount;

			DateTime refundDate = refund.Created;

			string summary =
				$"Stripe refund | Status: {refund.Status} | Refund: {refund.Id} | " +
				$"Amount: {refundAmount:0.00} {(refund.Currency ?? "usd").ToUpper()} | " +
				$"Original Session: {checkoutSessionId}";

			_payments.addPayment( new paymentModel {
				total=refundTotal,
				tax=refundTax,
				summary=summary,
				stripeID=refund.Id,
				reservationID=original.reservationID,
				userID=original.userID,
				paymentDate=refundDate
			});
		}

		private async Task HandleChargeRefundedAsync(Charge charge)
		{
			Console.WriteLine($"Charge refunded: {charge.Id}");
			Console.WriteLine($"Amount refunded total: {(charge.AmountRefunded / 100m):0.00}");

			// Optional:
			// This event is useful as a broad "some refund happened" signal.
			// If you already fully process refunds in refund.updated, you may choose
			// to only log this event and do nothing else here.

			await Task.CompletedTask;
		}

		private async Task<string?> FindCheckoutSessionIdByPaymentIntentAsync(string? paymentIntentId)
		{
			if (string.IsNullOrWhiteSpace(paymentIntentId))
				return null;

			var sessionService = new SessionService();
			var options = new SessionListOptions
			{
				PaymentIntent = paymentIntentId,
				Limit = 1
			};

			var sessions = await sessionService.ListAsync(options);
			return sessions.Data.FirstOrDefault()?.Id;
		}

		private static string GeneratePaymentSummary(Session session)
		{
			long amountCents = session.AmountTotal ?? 0;
			decimal amountDollars = amountCents / 100m;

			var parts = new List<string>();

			parts.Add("Stripe checkout");

			if (!string.IsNullOrWhiteSpace(session.PaymentStatus))
				parts.Add($"Status: {session.PaymentStatus}");

			parts.Add($"Amount: {amountDollars:0.00} {(session.Currency ?? "usd").ToUpper()}");

			if (!string.IsNullOrWhiteSpace(session.Id))
				parts.Add($"Session: {session.Id}");

			if (!string.IsNullOrWhiteSpace(session.PaymentIntentId))
				parts.Add($"PI: {session.PaymentIntentId}");

			if (!string.IsNullOrWhiteSpace(session.ClientReferenceId))
				parts.Add($"Ref: {session.ClientReferenceId}");

			if (session.CustomerDetails != null &&
				!string.IsNullOrWhiteSpace(session.CustomerDetails.Email))
			{
				parts.Add($"Email: {session.CustomerDetails.Email}");
			}

			return JoinWithLimit(parts, 255);
		}

		private static string JoinWithLimit(List<string> parts, int maxLength)
		{
			var result = parts[0];

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