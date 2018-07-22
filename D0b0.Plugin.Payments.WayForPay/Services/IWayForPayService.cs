using System.Collections.Generic;
using Nop.Services.Payments;

namespace D0b0.Plugin.Payments.WayForPay.Services
{
	public partial interface IWayForPayService
	{
		PaymentRequestModel BuildPaymentRequestModel(PostProcessPaymentRequest paymentRequest);
		bool IsValidSignature(IDictionary<string, object> data, string merchantSignature);
	}
}
