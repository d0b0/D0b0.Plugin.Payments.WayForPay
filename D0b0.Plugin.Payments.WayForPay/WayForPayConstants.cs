namespace D0b0.Plugin.Payments.WayForPay
{
	internal class WayForPayConstants
	{
		public static string PaymentUrl = "https://secure.wayforpay.com/pay";

		public static string ApiUrl = "https://api.wayforpay.com/api";

		public static string OkReasonCode = "1100";

		public static string OrderApprovedStatus = "Approved";

		public static string NotePaymentPrefix = "WayForPay IPN:";

		public static string InvoicePrefix = "invoice_";

		public static string SignatureSeparator = ";";

		public static string[] SigKeys = {
			"merchantAccount",
			"merchantDomainName",
			"orderReference",
			"orderDate",
			"amount",
			"currency",
			"productName",
			"productCount",
			"productPrice"
		};

		public static string[] ResSigKeys = {
			"merchantAccount",
			"orderReference",
			"amount",
			"currency",
			"authCode",
			"cardPan",
			"transactionStatus",
			"reasonCode"
		};

		public static string[] AckSigKeys = {
			"orderReference",
			"status",
			"time"
		};
	}
}
