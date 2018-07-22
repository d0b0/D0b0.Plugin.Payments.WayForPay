using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Services.Directory;
using Nop.Services.Payments;

namespace D0b0.Plugin.Payments.WayForPay.Services
{
	public partial class WayForPayService : IWayForPayService
	{
		private readonly WayForPayPaymentSettings _wayForPayPaymentSettings;
		private readonly CurrencySettings _currencySettings;
		private readonly IWebHelper _webHelper;
		private readonly ICurrencyService _currencyService;

		public WayForPayService(
			WayForPayPaymentSettings wayForPayPaymentSettings,
			CurrencySettings currencySettings,
			IWebHelper webHelper,
			ICurrencyService currencyService)
		{
			_wayForPayPaymentSettings = wayForPayPaymentSettings;
			_webHelper = webHelper;
			_currencyService = currencyService;
			_currencySettings = currencySettings;
		}

		public PaymentRequestModel BuildPaymentRequestModel(PostProcessPaymentRequest paymentRequest)
		{
			var model = new PaymentRequestModel
			{
				OrderReference = paymentRequest.Order.Id,
				OrderDate = (int)(paymentRequest.Order.CreatedOnUtc.Subtract(new DateTime(1970, 1, 1))).TotalSeconds,
				AuthorizationType = "SimpleSignature",
				MerchantAccount = _wayForPayPaymentSettings.MerchantAccount,
				MerchantDomainName = _wayForPayPaymentSettings.MerchantDomainName,
				MerchantTransactionSecureType = "AUTO",
				Amount = paymentRequest.Order.OrderTotal.ToString("0.00", CultureInfo.InvariantCulture),
				Currency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode,
				Language = "RU",
				ServiceUrl = _webHelper.GetStoreLocation(false),
				ReturnUrl = _webHelper.GetStoreLocation(false) + "Plugins/PaymentWayForPay/IPNHandler"
			};

			//products
			var orderProducts = paymentRequest.Order.OrderItems.ToList();
			var productNames = new List<string>();
			var productQty = new List<int>();
			var productPrices = new List<string>();

			foreach (OrderItem item in orderProducts)
			{
				productNames.Add(item.Product.Name);
				productPrices.Add(item.UnitPriceInclTax.ToString("0.00", CultureInfo.InvariantCulture));
				productQty.Add(item.Quantity);
			}

			model.ProductName = productNames.ToArray();
			model.ProductPrice = productPrices.ToArray();
			model.ProductCount = productQty.ToArray();

			// phone
			var phone = paymentRequest.Order.BillingAddress.PhoneNumber;
			if (phone.Length == 10)
			{
				phone = $"38{phone}";
			}
			else if (phone.Length == 11)
			{
				phone = $"3{phone}";
			}
			model.ClientPhone = phone;

			// client
			model.ClientFirstName = paymentRequest.Order.BillingAddress.FirstName;
			model.ClientLastName = paymentRequest.Order.BillingAddress.LastName;
			model.ClientEmail = paymentRequest.Order.BillingAddress.Email;
			model.ClientCity = paymentRequest.Order.BillingAddress.City;
			model.ClientAddress = paymentRequest.Order.BillingAddress.Address1;

			// country
			var billingCountry = paymentRequest.Order.BillingAddress.Country;
			model.ClientCountry = billingCountry != null ? billingCountry.ThreeLetterIsoCode : "";

			// signature
			Dictionary<string, object> sigDict = new Dictionary<string, object>
			{
				{"merchantAccount", model.MerchantAccount },
				{"merchantDomainName", model.MerchantDomainName },
				{"orderReference", model.OrderReference },
				{"orderDate", model.OrderDate },
				{"amount", model.Amount },
				{"currency", model.Currency },
				{"productName", model.ProductName },
				{"productCount", model.ProductCount },
				{"productPrice", model.ProductPrice }
			};
			model.MerchantSignature = GetSignature(sigDict, WayForPayConstants.KeysForSignature);

			return model;
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
