using Nop.Core.Configuration;

namespace D0b0.Plugin.Payments.WayForPay
{
	public class WayForPayPaymentSettings : ISettings
	{
		public string MerchantAccount { get; set; }
		public string MerchantSecretKey { get; set; }
		public string MerchantDomainName { get; set; }
		public decimal AdditionalFee { get; set; }
		public bool AdditionalFeePercentage { get; set; }
		public bool UseWidget { get; set; }
	}
}
