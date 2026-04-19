using RVfamcamp.Models;
using RVfamcamp.Services;

namespace RVfamcamp.Database
{
	public class PaymentRepo
	{
		private readonly DatabaseStatements _databaseStatements;

		public PaymentRepo(DatabaseStatements databaseStatements)
		{
			_databaseStatements = databaseStatements;
		}

		public List<paymentModel> getAllPaymentsForUser(int userId)
		{
			return _databaseStatements.getPaymentsByUserID(userId);
		}

		public bool addPayment(paymentModel payment)
		{
			_databaseStatements.AddPayment(payment.total, payment.tax, payment.summary, payment.stripeID, payment.reservationID);
			return true;
		}

		public paymentModel getPaymentByStripeID(string checkoutSessionId)
		{
			return _databaseStatements.getPaymentByStripeID(checkoutSessionId);
		}
	}
}
