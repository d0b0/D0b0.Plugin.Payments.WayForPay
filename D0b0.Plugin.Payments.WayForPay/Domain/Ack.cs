namespace D0b0.Plugin.Payments.WayForPay.Domain
{
	public class Ack
	{
		public string OrderReference { get; set; }
		public string Status { get; set; }
		public int Time { get; set; }
		public string Signature { get; set; }
	}
}
