using RVfamcamp.Models;

namespace RVfamcamp.Database
{
	public class PaymentRepo
	{
		static int curID = 0;

		List<paymentModel> paymentList = new();
		public List<paymentModel> getAllPayments()
		{
			return paymentList;
		}

		public List<paymentModel> getAllPaymentsForUser(int userId)
		{
			List<paymentModel> userPayments = new List<paymentModel>();
			foreach (var payment in paymentList)
			{
				if (payment.userID == userId)
				{
					userPayments.Add(payment);
				}
			}
			return userPayments;
		}

		public bool addPayment(paymentModel payment)
		{
			payment.id = curID++;
			paymentList.Add(payment);
			return true;
		}
	}
}
