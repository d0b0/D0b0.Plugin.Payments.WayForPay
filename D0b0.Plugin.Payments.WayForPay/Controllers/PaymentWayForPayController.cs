using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using D0b0.Plugin.Payments.WayForPay.Domain;
using D0b0.Plugin.Payments.WayForPay.Models;
using D0b0.Plugin.Payments.WayForPay.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;

namespace D0b0.Plugin.Payments.WayForPay.Controllers
{
	public class PaymentWayForPayController : BasePaymentController
	{
		private readonly WayForPayPaymentSettings _wayForPayPaymentSettings;
		private readonly PaymentSettings _paymentSettings;
		private readonly HttpContextBase _httpContext;
		private readonly ISettingService _settingService;
		private readonly IPaymentService _paymentService;
		private readonly IOrderService _orderService;
		private readonly IOrderProcessingService _orderProcessingService;
		private readonly ILocalizationService _localizationService;
		private readonly IWebHelper _webHelper;
		private readonly IWayForPayService _wayForPayService;

		public PaymentWayForPayController(
			WayForPayPaymentSettings wayForPayPaymentSettings,
			PaymentSettings paymentSettings,
			HttpContextBase httpContext,
			ISettingService settingService,
			IPaymentService paymentService,
			IOrderService orderService,
			IOrderProcessingService orderProcessingService,
			ILocalizationService localizationService,
			IWebHelper webHelper,
			IWayForPayService wayForPayService)
		{
			_wayForPayPaymentSettings = wayForPayPaymentSettings;
			_paymentSettings = paymentSettings;
			_httpContext = httpContext;
			_settingService = settingService;
			_paymentService = paymentService;
			_orderService = orderService;
			_orderProcessingService = orderProcessingService;
			_localizationService = localizationService;
			_webHelper = webHelper;
			_wayForPayService = wayForPayService;
		}

		public override IList<string> ValidatePaymentForm(FormCollection form)
		{
			var warnings = new List<string>();

			return warnings;
		}

		public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
		{
			var paymentInfo = new ProcessPaymentRequest();

			return paymentInfo;
		}

		[AdminAuthorize]
		[ChildActionOnly]
		public ActionResult Configure()
		{
			var model = new ConfigurationModel
			{
				MerchantAccount = _wayForPayPaymentSettings.MerchantAccount,
				MerchantSecretKey = _wayForPayPaymentSettings.MerchantSecretKey,
				MerchantDomainName = _wayForPayPaymentSettings.MerchantDomainName,
				AdditionalFee = _wayForPayPaymentSettings.AdditionalFee,
				AdditionalFeePercentage = _wayForPayPaymentSettings.AdditionalFeePercentage,
				UseWidget = _wayForPayPaymentSettings.UseWidget,
				SendInvoiceAfterTry = _wayForPayPaymentSettings.SendInvoiceAfterTry,
				InvoiceTimeout = _wayForPayPaymentSettings.InvoiceTimeout,
			};

			return View("~/Plugins/Payments.WayForPay/Views/WayForPay/Configure.cshtml", model);
		}

		[HttpPost]
		[AdminAuthorize]
		[ChildActionOnly]
		public ActionResult Configure(ConfigurationModel model)
		{
			if (!ModelState.IsValid)
			{
				return Configure();
			}

			_wayForPayPaymentSettings.MerchantAccount = model.MerchantAccount;
			_wayForPayPaymentSettings.MerchantSecretKey = model.MerchantSecretKey;
			_wayForPayPaymentSettings.MerchantDomainName = model.MerchantDomainName;
			_wayForPayPaymentSettings.AdditionalFee = model.AdditionalFee;
			_wayForPayPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
			_wayForPayPaymentSettings.UseWidget = model.UseWidget;
			_wayForPayPaymentSettings.SendInvoiceAfterTry = model.SendInvoiceAfterTry;
			_wayForPayPaymentSettings.InvoiceTimeout = model.InvoiceTimeout;

			_settingService.SaveSetting(_wayForPayPaymentSettings);
			SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

			return View("~/Plugins/Payments.WayForPay/Views/WayForPay/Configure.cshtml", model);
		}

		[ChildActionOnly]
		public ActionResult PaymentInfo()
		{
			return View("~/Plugins/Payments.WayForPay/Views/WayForPay/PaymentInfo.cshtml");
		}

		[ChildActionOnly]
		public ActionResult PublicInfo(string widgetZone, object additionalData = null)
		{
			var model = new PublicInfoModel();

			int orderId = (int)additionalData;
			if (orderId != 0)
			{
				var order = _orderService.GetOrderById(orderId);
				model.OrderId = orderId;
				model.ShowInvoiceButton = order.PaymentMethodSystemName == "Payments.WayForPay"
					&& order.PaymentStatusId != (int)PaymentStatus.Paid;
			}

			return View("~/Plugins/Payments.WayForPay/Views/WayForPay/PublicInfo.cshtml", model);
		}

		[HttpPost]
		[AdminAuthorize]
		[ChildActionOnly]
		[FormValueRequired("invoice_way4pay")]
		public async Task<ActionResult> PublicInfo(int orderId)
		{
			if (!ModelState.IsValid)
			{
				return View("~/Plugins/Payments.WayForPay/Views/WayForPay/PublicInfo.cshtml", new PublicInfoModel
				{
					ShowInvoiceButton = false
				});
			}

			var order = _orderService.GetOrderById(orderId);
			var request = _wayForPayService.BuildInvoiceRequest(order);
			var client = new HttpClient();
			string json = JsonConvert.SerializeObject(request, new JsonSerializerSettings
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver()
			});
			WriteOrderNote(order, $"[INVOICE] {FormatContent(json)}");

			var response = client.PostAsync(new Uri(WayForPayConstants.ApiUrl),
				new StringContent(json, Encoding.UTF8, "application/json")).Result;
			response.EnsureSuccessStatusCode();

			var content = await response.Content.ReadAsStringAsync();
			var msg = FormatContent(content);
			WriteOrderNote(order, msg);

			return View("~/Plugins/Payments.WayForPay/Views/WayForPay/PublicInfo.cshtml", new PublicInfoModel
			{
				OrderId = orderId,
				ShowInvoiceButton = false,
				Message = msg //_localizationService.GetResource("Plugins.Payments.WayForPay.SentInvoice")
			});
		}

		[HttpPost]
		[ValidateInput(false)]
		public ActionResult IPN()
		{
			var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.WayForPay") as WayForPayPaymentPlugin;

			if (processor == null ||
				!processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
				throw new NopException("WayForPay module cannot be loaded");

			var data = JsonConvert.DeserializeObject<Payment>(Request.Form[0]);

			if (data == null)
			{
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			}

			var orderIdStr = data.OrderReference.Replace(WayForPayConstants.InvoicePrefix, "");
			int orderId;
			if (!int.TryParse(orderIdStr, out orderId))
			{
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			}

			var order = _orderService.GetOrderById(orderId);
			if (order == null)
			{
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			}

			WriteOrderNote(order, FormatContent(Request.Form[0]));

			var collection = JsonConvert.DeserializeObject<IDictionary<string, object>>(Request.Form[0]);
			if (!_wayForPayService.IsValidSignature(collection, data.MerchantSignature))
			{
				WriteOrderNote(order, "Not valid signature");
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			}
			
			if (IsOrderPaid(data.ReasonCode, data.TransactionStatus) && _orderProcessingService.CanMarkOrderAsPaid(order))
			{
				WriteOrderNote(order, $"New payment status: {PaymentStatus.Paid}");
				_orderProcessingService.MarkOrderAsPaid(order);
			}

			WriteAck(data.OrderReference);
			return new HttpStatusCodeResult(HttpStatusCode.OK);
		}

		private string FormatContent(string content)
		{
			var obj = JsonConvert.DeserializeObject(content);
			return JsonConvert.SerializeObject(obj, Formatting.Indented);
		}

		private void WriteOrderNote(Order order, string note)
		{
			order.OrderNotes.Add(new OrderNote
			{
				Note = $"{WayForPayConstants.NotePaymentPrefix} {note}",
				DisplayToCustomer = false,
				CreatedOnUtc = DateTime.UtcNow
			});

			_orderService.UpdateOrder(order);
		}

		private bool IsOrderPaid(string reasonCode, string transactionStatus)
		{
			return !string.IsNullOrEmpty(reasonCode)
				&& reasonCode.Equals(WayForPayConstants.OkReasonCode, StringComparison.InvariantCultureIgnoreCase)
				&& transactionStatus.Equals(WayForPayConstants.OrderApprovedStatus, StringComparison.InvariantCultureIgnoreCase);
		}

		private void WriteAck(string orderReference)
		{
			var content = _wayForPayService.CreateAcknowledgement(orderReference);
			string json = JsonConvert.SerializeObject(content, new JsonSerializerSettings
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver()
			});

			_httpContext.Response.ContentType = "application/json; charset=utf-8";
			_httpContext.Response.Write(json);
			_httpContext.Response.End();
		}
	}
}
