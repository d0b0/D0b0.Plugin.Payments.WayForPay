namespace D0b0.Plugin.Payments.WayForPay.Domain
{
	public class InvoiceRequest : BaseRequest
	{
		private string orderRef;
		public override string OrderReference
		{
			get
			{
				return orderRef;
			}
			set
			{
				orderRef = WayForPayConstants.InvoicePrefix + value;
			}
		}

		public string TransactionType => "CREATE_INVOICE";
		public int ApiVersion => 1;
	}
}
