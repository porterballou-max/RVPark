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
			Console.WriteLine("Getting all payments for user with id: userId!");
			Console.WriteLine($"Parseing {paymentList.Count()} payments!");
			List<paymentModel> userPayments = new List<paymentModel>();
			foreach (var payment in paymentList)
			{
				Console.WriteLine($"Parsing Payment with id: {payment.userID} ");
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
