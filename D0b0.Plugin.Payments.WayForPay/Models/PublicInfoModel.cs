using Nop.Web.Framework.Mvc;

namespace D0b0.Plugin.Payments.WayForPay.Models
{
	public class PublicInfoModel : BaseNopModel
	{
		public int OrderId { get; set; }
		public bool ShowInvoiceButton { get; set; }
	}
}
