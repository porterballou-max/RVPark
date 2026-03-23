using System;
using System.ComponentModel.DataAnnotations;

namespace RVfamcamp.Models
{
	public class paymentModel
	{
		public int id {  get; set; }

		public int userID { get; set; }

		public string stripeID { get; set; }

		public decimal total {  get; set; }

		public DateTime paymentDate { get; set; }

		public string summary { get; set; }
	}
}
