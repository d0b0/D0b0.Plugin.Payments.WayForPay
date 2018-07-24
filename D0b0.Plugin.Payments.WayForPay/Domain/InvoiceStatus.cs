namespace D0b0.Plugin.Payments.WayForPay.Domain
{
	public class InvoiceStatus
	{
		public string MerchantAccount { get; set; }
		public string OrderReference { get; set; }
		public string MerchantSignature { get; set; }
		public string Amount { get; set; }
		public string Currency { get; set; }
		public string AuthCode { get; set; }
		public string Email { get; set; }
		public string Phone { get; set; }
		public string CreatedDate { get; set; }
		public string ProcessingDate { get; set; }
		public string CardPan { get; set; }
		public string CardType { get; set; }
		public string IssuerBankCountry { get; set; }
		public string IssuerBankName { get; set; }
		public string RecToken { get; set; }
		public string TransactionStatus { get; set; }
		public string Reason { get; set; }
		public string ReasonCode { get; set; }
		public string Fee { get; set; }
		public string PaymentSystem { get; set; }
	}
}
