using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using D0b0.Plugin.Payments.WayForPay.Domain;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Services.Directory;
using Nop.Services.Orders;

namespace D0b0.Plugin.Payments.WayForPay.Services
{
	public partial class WayForPayService : IWayForPayService
	{
		private readonly WayForPayPaymentSettings _wayForPayPaymentSettings;
		private readonly CurrencySettings _currencySettings;
		private readonly IWebHelper _webHelper;
		private readonly ICurrencyService _currencyService;
		private readonly IOrderService _orderService;

		public WayForPayService(
			WayForPayPaymentSettings wayForPayPaymentSettings,
			CurrencySettings currencySettings,
			IWebHelper webHelper,
			ICurrencyService currencyService,
			IOrderService orderService)
		{
			_wayForPayPaymentSettings = wayForPayPaymentSettings;
			_webHelper = webHelper;
			_currencyService = currencyService;
			_currencySettings = currencySettings;
			_orderService = orderService;
		}

		public PaymentRequest BuildPaymentRequest(Order order)
		{
			var request = BuildRequest<PaymentRequest>(order);
			request.MerchantTransactionSecureType = "AUTO";
			request.ReturnUrl = _webHelper.GetStoreLocation(false) + "Plugins/PaymentWayForPay/IPNHandler";

			request.ClientCity = order.BillingAddress.City;
			request.ClientAddress = order.BillingAddress.Address1;

			var billingCountry = order.BillingAddress.Country;
			request.ClientCountry = billingCountry != null ? billingCountry.ThreeLetterIsoCode : "";

			return request;
		}

		public InvoiceRequest BuildInvoiceRequest(Order order)
		{
			var request = BuildRequest<InvoiceRequest>(order);

			request.ServiceUrl = _webHelper.GetStoreLocation(false) + "Plugins/PaymentWayForPay/IPN";
			//request.ServiceUrl = "http://4793f44d.ngrok.io/Plugins/PaymentWayForPay/IPN";

			return request;
		}

		public bool IsValidSignature(IDictionary<string, object> data, string merchantSignature)
		{
			return GetSignature(data, WayForPayConstants.ResSigKeys) == merchantSignature;
		}

		public Ack CreateAcknowledgement(string orderRef)
		{
			var ack = new Ack
			{
				OrderReference = orderRef,
				Status = "accept",
				Time = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds,
			};

			Dictionary<string, object> sigDict = new Dictionary<string, object>
			{
				{"orderReference", ack.OrderReference },
				{"status", ack.Status },
				{"time", ack.Time }
			};
			ack.Signature = GetSignature(sigDict, WayForPayConstants.AckSigKeys);

			return ack;
		}

		private T BuildRequest<T>(Order order) where T : BaseRequest, new()
		{
			var request = new T()
			{
				OrderReference = order.Id.ToString(),
				OrderDate = (int)(order.CreatedOnUtc.Subtract(new DateTime(1970, 1, 1))).TotalSeconds,
				MerchantAuthType = "SimpleSignature",
				MerchantAccount = _wayForPayPaymentSettings.MerchantAccount,
				MerchantDomainName = _wayForPayPaymentSettings.MerchantDomainName,
				Amount = order.OrderTotal.ToString("0.00", CultureInfo.InvariantCulture),
				Currency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode,
				Language = "RU",
				ServiceUrl = _webHelper.GetStoreLocation(false)
			};

			//products
			var orderProducts = order.OrderItems.ToList();
			var productNames = new List<string>();
			var productQty = new List<int>();
			var productPrices = new List<string>();

			foreach (OrderItem item in orderProducts)
			{
				productNames.Add(item.Product.Name);
				productPrices.Add(item.UnitPriceInclTax.ToString("0.00", CultureInfo.InvariantCulture));
				productQty.Add(item.Quantity);
			}

			request.ProductName = productNames.ToArray();
			request.ProductPrice = productPrices.ToArray();
			request.ProductCount = productQty.ToArray();

			// phone
			var phone = order.BillingAddress.PhoneNumber;
			if (phone.Length == 10)
			{
				phone = $"38{phone}";
			}
			else if (phone.Length == 11)
			{
				phone = $"3{phone}";
			}
			request.ClientPhone = phone;

			// client
			request.ClientFirstName = order.BillingAddress.FirstName;
			request.ClientLastName = order.BillingAddress.LastName;
			request.ClientEmail = order.BillingAddress.Email;

			// signature
			Dictionary<string, object> sigDict = new Dictionary<string, object>
			{
				{"merchantAccount", request.MerchantAccount },
				{"merchantDomainName", request.MerchantDomainName },
				{"orderReference", request.OrderReference },
				{"orderDate", request.OrderDate },
				{"amount", request.Amount },
				{"currency", request.Currency },
				{"productName", request.ProductName },
				{"productCount", request.ProductCount },
				{"productPrice", request.ProductPrice }
			};
			request.MerchantSignature = GetSignature(sigDict, WayForPayConstants.SigKeys);

			return request;
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
