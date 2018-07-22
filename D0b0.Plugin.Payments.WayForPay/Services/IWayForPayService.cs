using System.Collections.Generic;

namespace D0b0.Plugin.Payments.WayForPay.Services
{
	public partial interface IWayForPayService
	{
		string GetRequestSignature(IDictionary<string, object> data);
		bool IsValidSignature(IDictionary<string, object> data, string merchantSignature);
	}
}
