using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace D0b0.Plugin.Payments.WayForPay.Models
{
	public class ConfigurationModel : BaseNopModel
	{
		[NopResourceDisplayName("Plugins.Payments.WayForPay.MerchantAccount")]
		[Required]
		public string MerchantAccount { get; set; }

		[NopResourceDisplayName("Plugins.Payments.WayForPay.MerchantSecretKey")]
		[Required]
		public string MerchantSecretKey { get; set; }

		[NopResourceDisplayName("Plugins.Payments.WayForPay.MerchantDomainName")]
		[Required]
		public string MerchantDomainName { get; set; }

		[NopResourceDisplayName("Plugins.Payments.WayForPay.AdditionalFee")]
		public decimal AdditionalFee { get; set; }

		[NopResourceDisplayName("Plugins.Payments.WayForPay.AdditionalFeePercentage")]
		public bool AdditionalFeePercentage { get; set; }

		[NopResourceDisplayName("Plugins.Payments.WayForPay.UseWidget")]
		public bool UseWidget { get; set; }

		[NopResourceDisplayName("Plugins.Payments.WayForPay.SendInvoice")]
		public bool SendInvoice { get; set; }

		/// <summary>
		/// Invoice timeout in seconds.
		/// 60 - 1 minute
		/// 2592000 - 30 days
		/// </summary>
		[NopResourceDisplayName("Plugins.Payments.WayForPay.InvoiceTimeout")]
		[Required, Range(60, 2592000)]
		public int InvoiceTimeout { get; set; }
	}
}
