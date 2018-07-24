using System.Collections.Generic;
using D0b0.Plugin.Payments.WayForPay.Domain;
using Nop.Core.Domain.Orders;

namespace D0b0.Plugin.Payments.WayForPay.Services
{
	public partial interface IWayForPayService
	{
		PaymentRequest BuildPaymentRequest(Order order);
		InvoiceRequest BuildInvoiceRequest(Order order);
		bool IsValidSignature(IDictionary<string, object> data, string merchantSignature);
		Ack CreateAcknowledgement(string orderRef);
	}
}
