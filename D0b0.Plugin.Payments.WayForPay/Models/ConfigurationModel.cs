using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace D0b0.Plugin.Payments.WayForPay.Models
{
	public class ConfigurationModel : BaseNopModel
	{
		[NopResourceDisplayName("Plugins.Payments.WayForPay.MerchantAccount")]
		public string MerchantAccount { get; set; }

		[NopResourceDisplayName("Plugins.Payments.WayForPay.MerchantSecretKey")]
		public string MerchantSecretKey { get; set; }

		[NopResourceDisplayName("Plugins.Payments.WayForPay.MerchantDomainName")]
		public string MerchantDomainName { get; set; }

		[NopResourceDisplayName("Plugins.Payments.WayForPay.AdditionalFee")]
		public decimal AdditionalFee { get; set; }

		[NopResourceDisplayName("Plugins.Payments.WayForPay.AdditionalFeePercentage")]
		public bool AdditionalFeePercentage { get; set; }

		[NopResourceDisplayName("Plugins.Payments.WayForPay.UseWidget")]
		public bool UseWidget { get; set; }
	}
}
