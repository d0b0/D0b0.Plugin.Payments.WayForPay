namespace D0b0.Plugin.Payments.WayForPay.Domain
{
	public class PaymentRequest : BaseRequest
	{
		public override string OrderReference { get; set; }
		public string MerchantTransactionSecureType { get; set; }
		public string ClientAddress { get; set; }
		public string ClientCity { get; set; }
		public string ClientCountry { get; set; }
		public string ReturnUrl { get; set; }
	}
}
