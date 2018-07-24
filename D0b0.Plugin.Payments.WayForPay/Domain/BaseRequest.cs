namespace D0b0.Plugin.Payments.WayForPay.Domain
{
	public abstract class BaseRequest
	{
		public string MerchantAccount { get; set; }
		public string MerchantAuthType { get; set; }
		public string MerchantDomainName { get; set; }
		public string MerchantSignature { get; set; }
		public string Language { get; set; }
		public string ServiceUrl { get; set; }
		public int OrderDate { get; set; }
		public string Amount { get; set; }
		public string Currency { get; set; }
		public string[] ProductName { get; set; }
		public string[] ProductPrice { get; set; }
		public int[] ProductCount { get; set; }
		public string ClientFirstName { get; set; }
		public string ClientLastName { get; set; }
		public string ClientEmail { get; set; }
		public string ClientPhone { get; set; }
		public abstract string OrderReference { get; set; }
	}
}
