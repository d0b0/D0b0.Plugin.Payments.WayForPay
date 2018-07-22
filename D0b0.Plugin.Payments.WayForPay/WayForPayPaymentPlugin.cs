using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Routing;
using D0b0.Plugin.Payments.WayForPay.Controllers;
using D0b0.Plugin.Payments.WayForPay.Services;
using Newtonsoft.Json;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Services.Cms;
using Nop.Services.Orders;
using Nop.Services.Payments;

namespace D0b0.Plugin.Payments.WayForPay
{
	public class WayForPayPaymentPlugin : BasePlugin, IPaymentMethod, IWidgetPlugin
	{
		private readonly WayForPayPaymentSettings _wayForPayPaymentSettings;
		private readonly HttpContextBase _httpContext;
		private readonly IOrderTotalCalculationService _orderTotalCalculationService;
		private readonly IWayForPayService _wayForPayService;

		public WayForPayPaymentPlugin(
			WayForPayPaymentSettings wayForPayPaymentSettings,
			HttpContextBase httpContext,
			IOrderTotalCalculationService orderTotalCalculationService,
			IWayForPayService wayForPayService)
		{
			_wayForPayPaymentSettings = wayForPayPaymentSettings;
			_orderTotalCalculationService = orderTotalCalculationService;
			_httpContext = httpContext;
			_wayForPayService = wayForPayService;
		}

		public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
		{
			return new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Pending };
		}

		public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
		{
			var model = _wayForPayService.BuildPaymentRequestModel(postProcessPaymentRequest);

			if (_wayForPayPaymentSettings.UseWidget)
			{
				ProcessWidgetPayment(model);
				return;
			}

			ProcessRedirectPayment(model);
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

		private void ProcessWidgetPayment(PaymentRequestModel model)
		{
			var config = new
			{
				merchantAccount = model.MerchantAccount,
				merchantDomainName = model.MerchantDomainName,
				authorizationType = model.AuthorizationType,
				merchantSignature = model.MerchantSignature,
				merchantTransactionSecureType = model.MerchantTransactionSecureType,
				orderReference = model.OrderReference,
				orderDate = model.OrderDate,
				amount = model.Amount,
				currency = model.Currency,
				productName = model.ProductName,
				productPrice = model.ProductPrice,
				productCount = model.ProductCount,
				clientFirstName = model.ClientFirstName,
				clientLastName = model.ClientLastName,
				clientEmail = model.ClientEmail,
				clientPhone = model.ClientPhone,
				language = model.Language,
			};
			var script = "<script type=\"text/javascript\">" +
				"var config = " + JsonConvert.SerializeObject(config) + ";" +
				"var data = undefined;" +
				"var wayforpay = new Wayforpay();" +
				"var pay = function () {" +
				"wayforpay.run(config," +
				"function (response) { data = response; }," +
				"function (response) { }," +
				"function (response) { });" +
				"}; pay(); " +
				"window.addEventListener(\"message\", function(event){ " +
				"if(event.data === 'WfpWidgetEventClose') { $.redirect('" + model.ReturnUrl + "', data || config); } });" +
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
		}

		private void ProcessRedirectPayment(PaymentRequestModel model)
		{
			Dictionary<string, object> postData = new Dictionary<string, object>
			{
				{"orderReference", model.OrderReference},
				{"orderDate", model.OrderDate},
				{"merchantAuthType", model.AuthorizationType},
				{"merchantAccount", model.MerchantAccount},
				{"merchantDomainName", model.MerchantDomainName},
				{"merchantTransactionSecureType", model.MerchantTransactionSecureType},
				{"amount", model.Amount},
				{"currency", model.Currency},
				{"serviceUrl", model.ServiceUrl},
				{"returnUrl", model.ReturnUrl},
				{"language", model.Language},
				{"productName", model.ProductName},
				{"productPrice", model.ProductPrice},
				{"productCount", model.ProductCount},
				{"clientFirstName", model.ClientFirstName},
				{"clientLastName", model.ClientLastName},
				{"clientPhone", model.ClientPhone},
				{"clientEmail", model.ClientEmail},
				{"clientCity", model.ClientCity},
				{"clientAddress", model.ClientAddress},
				{"clientCountry", model.ClientCountry},
				{"merchantSignature", model.MerchantSignature}
			};

			var postForm = BuildPostForm(WayForPayConstants.PaymentUrl, postData);
			_httpContext.Response.Write(postForm);
			_httpContext.Response.End();
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
	}
}
