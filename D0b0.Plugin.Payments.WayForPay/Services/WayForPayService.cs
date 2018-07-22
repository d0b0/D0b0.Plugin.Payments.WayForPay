using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace D0b0.Plugin.Payments.WayForPay.Services
{
	public partial class WayForPayService : IWayForPayService
	{
		private readonly WayForPayPaymentSettings _wayForPayPaymentSettings;

		public WayForPayService(WayForPayPaymentSettings wayForPayPaymentSettings)
		{
			_wayForPayPaymentSettings = wayForPayPaymentSettings;
		}

		public string GetRequestSignature(IDictionary<string, object> data)
		{
			return GetSignature(data, WayForPayConstants.KeysForSignature);
		}

		public bool IsValidSignature(IDictionary<string, object> data, string merchantSignature)
		{
			return GetSignature(data, WayForPayConstants.KeysForResponseSignature) == merchantSignature;
		}

		private string GetSignature(IDictionary<string, object> data, string[] keys)
		{
			var items = new List<object>();
			foreach (var item in keys)
			{
				if (!data.ContainsKey(item))
				{
					items.Add(string.Empty);
					continue;
				}

				var array = data[item] as Array;
				if (array != null)
				{
					foreach (var subItem in array)
					{
						items.Add(subItem);
					}
					continue;
				}
				items.Add(data[item]);
			}

			var key = Encoding.UTF8.GetBytes(_wayForPayPaymentSettings.MerchantSecretKey);
			var value = Encoding.UTF8.GetBytes(string.Join(WayForPayConstants.SignatureSeparator, items));
			using (var hmacmd5 = new HMACMD5(key))
			{
				hmacmd5.ComputeHash(value);
				return BitConverter.ToString(hmacmd5.Hash).Replace("-", "").ToLower();
			}
		}
	}
}
