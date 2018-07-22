namespace D0b0.Plugin.Payments.WayForPay
{
	internal class WayForPayConstants
	{
		public static string PaymentUrl = "https://secure.wayforpay.com/pay";

		public static string OkReasonCode = "1100";

		public static string OrderStatusApproved = "Approved";

		public static string PaymentMethodPrefix = "WayForPay IPN:";

		public static string SignatureSeparator = ";";

		public static string[] KeysForSignature = {
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

		public static string[] KeysForResponseSignature = {
			"merchantAccount",
			"orderReference",
			"amount",
			"currency",
			"authCode",
			"cardPan",
			"transactionStatus",
			"reasonCode"
		};
	}
}
