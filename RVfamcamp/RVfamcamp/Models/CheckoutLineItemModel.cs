namespace RVfamcamp.Models
{
	public class CheckoutLineItemModel
	{
		public string name { get; set; } = "";
		public decimal unitAmount { get; set; }
		public long quantity { get; set; } = 1;
	}
}