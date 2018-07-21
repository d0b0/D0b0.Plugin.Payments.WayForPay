using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Routing;
using D0b0.Plugin.Payments.WayForPay.Controllers;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Services.Cms;
using Nop.Services.Directory;
using Nop.Services.Orders;
using Nop.Services.Payments;

namespace D0b0.Plugin.Payments.WayForPay
{
	public class WayForPayPaymentPlugin : BasePlugin, IPaymentMethod, IWidgetPlugin
	{
		private const string SignatureSeparator = ";";
		private static readonly string[] KeysForSignature = {
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
		private static readonly string[] KeysForResponseSignature = {
			"merchantAccount",
			"orderReference",
			"amount",
			"currency",
			"authCode",
			"cardPan",
			"transactionStatus",
			"reasonCode"
		};

		private readonly WayForPayPaymentSettings _wayForPayPaymentSettings;
		private readonly IOrderTotalCalculationService _orderTotalCalculationService;
		private readonly IWebHelper _webHelper;
		private readonly ICurrencyService _currencyService;
		private readonly CurrencySettings _currencySettings;
		private readonly HttpContextBase _httpContext;

		public WayForPayPaymentPlugin(
			WayForPayPaymentSettings wayForPayPaymentSettings,
			IOrderTotalCalculationService orderTotalCalculationService,
			IWebHelper webHelper,
			ICurrencyService currencyService,
			CurrencySettings currencySettings,
			HttpContextBase httpContext)
		{
			_wayForPayPaymentSettings = wayForPayPaymentSettings;
			_orderTotalCalculationService = orderTotalCalculationService;
			_webHelper = webHelper;
			_currencyService = currencyService;
			_currencySettings = currencySettings;
			_httpContext = httpContext;
		}

		public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
		{
			return new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Pending };
		}

		public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
		{
			var config = ConvertToConfig(postProcessPaymentRequest);

			if (_wayForPayPaymentSettings.UseWidget)
			{
				var widgetConfig = new
				{
					merchantAccount = config.MerchantAccount,
					merchantDomainName = config.MerchantDomainName,
					authorizationType = config.AuthorizationType,
					merchantSignature = config.MerchantSignature,
					merchantTransactionSecureType = config.MerchantTransactionSecureType,
					orderReference = config.OrderReference,
					orderDate = config.OrderDate,
					amount = config.Amount,
					currency = config.Currency,
					productName = config.ProductName,
					productPrice = config.ProductPrice,
					productCount = config.ProductCount,
					clientFirstName = config.ClientFirstName,
					clientLastName = config.ClientLastName,
					clientEmail = config.ClientEmail,
					clientPhone = config.ClientPhone,
					language = config.Language,
				};
				var script = "<script type=\"text/javascript\">" +
					"var config = " + JsonConvert.SerializeObject(widgetConfig) + ";" +
					"var data = undefined;" +
					"var wayforpay = new Wayforpay();" +
					"var pay = function () {" +
					"wayforpay.run(config," +
					"function (response) { data = response; }," +
					"function (response) { }," +
					"function (response) { });" +
					"}; pay(); " +
					"window.addEventListener(\"message\", function(event){if(event.data === 'WfpWidgetEventClose') { $.redirect('" + config.ReturnUrl + "', data || config); } });" +
					"</script>";
				var content = new
				{
					update_section = new
					{
						name = "confirm-order",
						html = script
					}
				};
				string json = JsonConvert.SerializeObject(content);
				_httpContext.Response.ContentType = "application/json; charset=utf-8";
				_httpContext.Response.Write(json);
				_httpContext.Response.End();
				return;
			}

			var purchaseUrl = "https://secure.wayforpay.com/pay";

			Dictionary<string, object> postData = new Dictionary<string, object>
			{
				{"orderReference", config.OrderReference},
				{"orderDate", config.OrderDate},
				{"merchantAuthType", config.AuthorizationType},
				{"merchantAccount", config.MerchantAccount},
				{"merchantDomainName", config.MerchantDomainName},
				{"merchantTransactionSecureType", config.MerchantTransactionSecureType},
				{"amount", config.Amount},
				{"currency", config.Currency},
				{"serviceUrl", config.ServiceUrl},
				{"returnUrl", config.ReturnUrl},
				{"language", config.Language},
				{"productName", config.ProductName},
				{"productPrice", config.ProductPrice},
				{"productCount", config.ProductCount},
				{"clientFirstName", config.ClientFirstName},
				{"clientLastName", config.ClientLastName},
				{"clientPhone", config.ClientPhone},
				{"clientEmail", config.ClientEmail},
				{"clientCity", config.ClientCity},
				{"clientAddress", config.ClientAddress},
				{"clientCountry", config.ClientCountry},
				{"merchantSignature", config.MerchantSignature}
			};

			var postForm = BuildPostForm(purchaseUrl, postData);
			HttpContext.Current.Response.Write(postForm);
			HttpContext.Current.Response.End();
		}

		public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
		{
			return false;
		}

		public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
		{
			var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
				_wayForPayPaymentSettings.AdditionalFee, _wayForPayPaymentSettings.AdditionalFeePercentage);
			return result;
		}

		public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
		{
			var result = new CapturePaymentResult();
			result.AddError("Capture method not supported");

			return result;
		}

		public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
		{
			var result = new RefundPaymentResult();
			result.AddError("Refund method not supported");

			return result;
		}

		public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
		{
			var result = new VoidPaymentResult();
			result.AddError("Void method not supported");

			return result;
		}

		public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
		{
			var result = new ProcessPaymentResult();
			result.AddError("Recurring payment not supported");

			return result;
		}

		public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
		{
			var result = new CancelRecurringPaymentResult();
			result.AddError("Recurring payment not supported");

			return result;
		}

		public bool CanRePostProcessPayment(Order order)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			//do not allow reposting (it can take up to several hours until your order is reviewed
			return false;
		}

		public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
		{
			actionName = "Configure";
			controllerName = "PaymentWayForPay";
			routeValues = new RouteValueDictionary {
				{ "Namespaces", "D0b0.Plugin.Payments.WayForPay.Controllers" },
				{ "area", null }
			};
		}

		public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
		{
			actionName = "PaymentInfo";
			controllerName = "PaymentWayForPay";
			routeValues = new RouteValueDictionary {
				{ "Namespaces", "D0b0.Plugin.Payments.WayForPay.Controllers" },
				{ "area", null }
			};
		}

		public Type GetControllerType()
		{
			return typeof(PaymentWayForPayController);
		}

		#region Properies

		/// <summary>
		/// Gets a value indicating whether capture is supported
		/// </summary>
		public bool SupportCapture => false;

		/// <summary>
		/// Gets a value indicating whether partial refund is supported
		/// </summary>
		public bool SupportPartiallyRefund => false;

		/// <summary>
		/// Gets a value indicating whether refund is supported
		/// </summary>
		public bool SupportRefund => false;

		/// <summary>
		/// Gets a value indicating whether void is supported
		/// </summary>
		public bool SupportVoid => false;

		/// <summary>
		/// Gets a recurring payment type of payment method
		/// </summary>
		public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

		/// <summary>
		/// Gets a payment method type
		/// </summary>
		public PaymentMethodType PaymentMethodType
		{
			get
			{
				return _wayForPayPaymentSettings.UseWidget ? PaymentMethodType.Standard : PaymentMethodType.Redirection;
			}
		}

		/// <summary>
		/// Gets a value indicating whether we should display a payment information page for this plugin
		/// </summary>
		public bool SkipPaymentInfo => false;

		#endregion

		public IList<string> GetWidgetZones()
		{
			return new List<string> { "admin_order_payment_info" };
		}

		public void GetDisplayWidgetRoute(string widgetZone, out string actionName, out string controllerName, out RouteValueDictionary routeValues)
		{
			actionName = "PublicInfo";
			controllerName = "PaymentWayForPay";
			routeValues = new RouteValueDictionary
			{
				{"Namespaces", "D0b0.Plugin.Payments.WayForPay.Controllers"},
				{"area", null},
				{"widgetZone", widgetZone}
			};
		}

		public string GetResponseSignature(IDictionary<string, object> data)
		{
			return GetSignature(data, KeysForResponseSignature);
		}

		private WayForPayConfig ConvertToConfig(PostProcessPaymentRequest paymentRequest)
		{
			var config = new WayForPayConfig
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

			config.ProductName = productNames.ToArray();
			config.ProductPrice = productPrices.ToArray();
			config.ProductCount = productQty.ToArray();

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
			config.ClientPhone = phone;

			// client
			config.ClientFirstName = paymentRequest.Order.BillingAddress.FirstName;
			config.ClientLastName = paymentRequest.Order.BillingAddress.LastName;
			config.ClientEmail = paymentRequest.Order.BillingAddress.Email;
			config.ClientCity = paymentRequest.Order.BillingAddress.City;
			config.ClientAddress = paymentRequest.Order.BillingAddress.Address1;

			// country
			var billingCountry = paymentRequest.Order.BillingAddress.Country;
			config.ClientCountry = billingCountry != null ? billingCountry.ThreeLetterIsoCode : "";

			// signature
			Dictionary<string, object> sigDict = new Dictionary<string, object>
			{
				{"merchantAccount", config.MerchantAccount },
				{"merchantDomainName", config.MerchantDomainName },
				{"orderReference", config.OrderReference },
				{"orderDate", config.OrderDate },
				{"amount", config.Amount },
				{"currency", config.Currency },
				{"productName", config.ProductName },
				{"productCount", config.ProductCount },
				{"productPrice", config.ProductPrice }
			};
			config.MerchantSignature = GetRequestSignature(sigDict);

			return config;
		}

		private string BuildPostForm(string url, IDictionary<string, object> data)
		{
			string str = "__PostForm";
			StringBuilder formBuilder = new StringBuilder();
			formBuilder.Append(string.Format("<form id=\"{0}\" name=\"{0}\" action=\"{1}\" method=\"POST\">", str, url));
			foreach (KeyValuePair<string, object> keyValuePair in data)
			{
				var array = keyValuePair.Value as Array;
				if (array == null)
				{
					formBuilder.Append($"<input type=\"hidden\" name=\"{keyValuePair.Key}\" value=\"{keyValuePair.Value}\"/>");
					continue;
				}

				foreach (var item in array)
				{
					formBuilder.Append($"<input type=\"hidden\" name=\"{keyValuePair.Key}[]\" value=\"{item}\"/>");
				}
			}
			formBuilder.Append("</form>");
			StringBuilder scriptBuilder = new StringBuilder();
			scriptBuilder.Append("<script language=\"javascript\">");
			scriptBuilder.Append(string.Format("var v{0}=document.{0};", str));
			scriptBuilder.Append($"v{str}.submit();");
			scriptBuilder.Append("</script>");
			return formBuilder + scriptBuilder.ToString();
		}

		private string GetRequestSignature(IDictionary<string, object> data)
		{
			return GetSignature(data, KeysForSignature);
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
			var value = Encoding.UTF8.GetBytes(string.Join(SignatureSeparator, items));
			using (var hmacmd5 = new HMACMD5(key))
			{
				hmacmd5.ComputeHash(value);
				return BitConverter.ToString(hmacmd5.Hash).Replace("-", "").ToLower();
			}
		}
	}
}
