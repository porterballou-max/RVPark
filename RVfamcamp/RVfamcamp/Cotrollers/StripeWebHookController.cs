using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RVfamcamp.Configuration;
using RVfamcamp.Database;
using RVfamcamp.Models;
using RVfamcamp.Services;
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
		private readonly EmailService _email;
		private readonly DatabaseStatements _db;

		public StripeWebHookController(PaymentRepo payments, IOptions<StripeSettings> config, EmailService email, DatabaseStatements db)
		{
			_payments = payments;
			_config = config.Value;
			_email = email;
			_db = db;
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
			else
				Console.WriteLine("Could not parse client reference id from the session");

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
				Console.WriteLine($"Payment added to DB. With client id = {toAdd.userID} and checkout session id: {session.Id}");
			}
			else
			{
				throw new Exception("Failed to save payment to database.");
			}

			var user = _db.GetUserById(userId);
			var reservation = _db.GetReservationById(toAdd.reservationID);

			string reservationHtml = "";
			if (reservation != null)
			{
				reservationHtml = $@"
			<tr>
				<td style='padding: 8px; border: 1px solid #ddd;'><strong>Reservation ID</strong></td>
				<td style='padding: 8px; border: 1px solid #ddd;'>{reservation.reservationId}</td>
			</tr>
			<tr>
				<td style='padding: 8px; border: 1px solid #ddd;'><strong>Confirmation Number</strong></td>
				<td style='padding: 8px; border: 1px solid #ddd;'>{reservation.confirmationNumber}</td>
			</tr>
			<tr>
				<td style='padding: 8px; border: 1px solid #ddd;'><strong>Check-In</strong></td>
				<td style='padding: 8px; border: 1px solid #ddd;'>{reservation.startDate:MMMM d, yyyy}</td>
			</tr>
			<tr>
				<td style='padding: 8px; border: 1px solid #ddd;'><strong>Check-Out</strong></td>
				<td style='padding: 8px; border: 1px solid #ddd;'>{reservation.endDate:MMMM d, yyyy}</td>
			</tr>";
			}

			await _email.SendEmail(
				user.Email,
				"Payment Confirmation - RV Fam Camp",
				$@"
		<div style='font-family: Arial, Helvetica, sans-serif; background-color: #f7f7f7; padding: 24px;'>
			<div style='max-width: 700px; margin: 0 auto; background-color: #ffffff; border: 1px solid #dddddd; border-radius: 8px; overflow: hidden;'>
				<div style='background-color: #2f6f3e; color: white; padding: 20px 24px;'>
					<h2 style='margin: 0;'>Payment Confirmation</h2>
				</div>

				<div style='padding: 24px; color: #333333;'>
					<p style='margin-top: 0;'>Hello {user.FirstName},</p>

					<p>
						Thank you for your payment. This email confirms that we successfully received your payment for your RV Fam Camp reservation.
					</p>

					<table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
						<tr>
							<td style='padding: 8px; border: 1px solid #ddd;'><strong>Payment Date</strong></td>
							<td style='padding: 8px; border: 1px solid #ddd;'>{toAdd.paymentDate:MMMM d, yyyy h:mm tt}</td>
						</tr>
						<tr>
							<td style='padding: 8px; border: 1px solid #ddd;'><strong>Stripe Session ID</strong></td>
							<td style='padding: 8px; border: 1px solid #ddd;'>{session.Id}</td>
						</tr>
						<tr>
							<td style='padding: 8px; border: 1px solid #ddd;'><strong>Payment Status</strong></td>
							<td style='padding: 8px; border: 1px solid #ddd;'>{session.PaymentStatus}</td>
						</tr>
						<tr>
							<td style='padding: 8px; border: 1px solid #ddd;'><strong>Total Paid</strong></td>
							<td style='padding: 8px; border: 1px solid #ddd;'>{toAdd.total.ToString("C")}</td>
						</tr>
						{reservationHtml}
					</table>

					<p>
						You can keep this email for your records. If you need help with your reservation, please reference your confirmation number when contacting support.
					</p>

					<p style='margin-bottom: 24px;'>
						View the site here:
						<a href='https://cs3750-dusklabs-rvfamcamp-hba9fkevc6h2a4e4.centralus-01.azurewebsites.net/' target='_blank'>
							RV Fam Camp
						</a>
					</p>

					<hr style='border: none; border-top: 1px solid #dddddd; margin: 24px 0;' />

					<p style='font-size: 12px; color: #777777; margin-bottom: 0;'>
						Thank you,<br />
						Dusk Labs - CS 3750 - Spring Semester 2026
					</p>
				</div>
			</div>
		</div>"
			);
		}

		private async Task HandleRefundUpdatedAsync(Refund refund)
		{
			if (!string.Equals(refund.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
				return;

			string? checkoutSessionId = await FindCheckoutSessionIdByPaymentIntentAsync(refund.PaymentIntentId);
			if (string.IsNullOrWhiteSpace(checkoutSessionId))
				return;

			Console.WriteLine($"Getting payment for checkout id: {checkoutSessionId}");
			var original = _payments.getPaymentByStripeID(checkoutSessionId);
			Console.WriteLine($"Found: {original.userID}");

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

			_payments.addPayment(new paymentModel
			{
				total = refundTotal,
				tax = refundTax,
				summary = summary,
				stripeID = refund.Id,
				reservationID = original.reservationID,
				userID = original.userID,
				paymentDate = refundDate
			});
			Console.WriteLine($"USER: {original.userID}");
			var user = _db.GetUserById(original.userID);
			Console.WriteLine($"USER: {user == null}");
			if (user != null)
				Console.WriteLine($"USER: {user.FirstName}");
			var reservation = _db.GetReservationById(original.reservationID);

			string reservationHtml = "";
			if (reservation != null)
			{
				reservationHtml = $@"
			<tr>
				<td style='padding: 8px; border: 1px solid #ddd;'><strong>Reservation ID</strong></td>
				<td style='padding: 8px; border: 1px solid #ddd;'>{reservation.reservationId}</td>
			</tr>
			<tr>
				<td style='padding: 8px; border: 1px solid #ddd;'><strong>Confirmation Number</strong></td>
				<td style='padding: 8px; border: 1px solid #ddd;'>{reservation.confirmationNumber}</td>
			</tr>
			<tr>
				<td style='padding: 8px; border: 1px solid #ddd;'><strong>Check-In</strong></td>
				<td style='padding: 8px; border: 1px solid #ddd;'>{reservation.startDate:MMMM d, yyyy}</td>
			</tr>
			<tr>
				<td style='padding: 8px; border: 1px solid #ddd;'><strong>Check-Out</strong></td>
				<td style='padding: 8px; border: 1px solid #ddd;'>{reservation.endDate:MMMM d, yyyy}</td>
			</tr>";
			}

			await _email.SendEmail(
				user.Email,
				"Refund Confirmation - RV Fam Camp",
				$@"
		<div style='font-family: Arial, Helvetica, sans-serif; background-color: #f7f7f7; padding: 24px;'>
			<div style='max-width: 700px; margin: 0 auto; background-color: #ffffff; border: 1px solid #dddddd; border-radius: 8px; overflow: hidden;'>
				<div style='background-color: #8b2e2e; color: white; padding: 20px 24px;'>
					<h2 style='margin: 0;'>Refund Confirmation</h2>
				</div>

				<div style='padding: 24px; color: #333333;'>
					<p style='margin-top: 0;'>Hello {user.FirstName},</p>

					<p>
						Your refund has been successfully processed for your RV Fam Camp reservation.
					</p>

					<table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
						<tr>
							<td style='padding: 8px; border: 1px solid #ddd;'><strong>Refund Date</strong></td>
							<td style='padding: 8px; border: 1px solid #ddd;'>{refundDate:MMMM d, yyyy h:mm tt}</td>
						</tr>
						<tr>
							<td style='padding: 8px; border: 1px solid #ddd;'><strong>Refund ID</strong></td>
							<td style='padding: 8px; border: 1px solid #ddd;'>{refund.Id}</td>
						</tr>
						<tr>
							<td style='padding: 8px; border: 1px solid #ddd;'><strong>Original Session ID</strong></td>
							<td style='padding: 8px; border: 1px solid #ddd;'>{checkoutSessionId}</td>
						</tr>
						<tr>
							<td style='padding: 8px; border: 1px solid #ddd;'><strong>Refund Status</strong></td>
							<td style='padding: 8px; border: 1px solid #ddd;'>{refund.Status}</td>
						</tr>
						<tr>
							<td style='padding: 8px; border: 1px solid #ddd;'><strong>Refund Amount</strong></td>
							<td style='padding: 8px; border: 1px solid #ddd;'>{refundAmount.ToString("C")}</td>
						</tr>
						{reservationHtml}
					</table>

					<p>
						Please allow a little time for the refund to appear on your bank or card statement, depending on your payment provider.
					</p>

					<p style='margin-bottom: 24px;'>
						Visit:
						<a href='https://cs3750-dusklabs-rvfamcamp-hba9fkevc6h2a4e4.centralus-01.azurewebsites.net/' target='_blank'>
							RV Fam Camp
						</a>
					</p>

					<hr style='border: none; border-top: 1px solid #dddddd; margin: 24px 0;' />

					<p style='font-size: 12px; color: #777777; margin-bottom: 0;'>
						Thank you,<br />
						Dusk Labs - CS 3750 - Spring Semester 2026
					</p>
				</div>
			</div>
		</div>"
			);
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