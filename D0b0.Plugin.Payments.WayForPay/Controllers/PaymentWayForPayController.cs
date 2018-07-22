using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using D0b0.Plugin.Payments.WayForPay.Models;
using D0b0.Plugin.Payments.WayForPay.Services;
using Nop.Admin.Models.Orders;
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
			ISettingService settingService,
			IPaymentService paymentService,
			IOrderService orderService,
			IOrderProcessingService orderProcessingService,
			ILocalizationService localizationService,
			IWebHelper webHelper,
			IWayForPayService wayForPayService)
		{
			_settingService = settingService;
			_paymentService = paymentService;
			_orderService = orderService;
			_orderProcessingService = orderProcessingService;
			_localizationService = localizationService;
			_wayForPayPaymentSettings = wayForPayPaymentSettings;
			_webHelper = webHelper;
			_paymentSettings = paymentSettings;
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
			};

			return View("~/Plugins/Payments.WayForPay/Views/PaymentWayForPay/Configure.cshtml", model);
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
			_settingService.SaveSetting(_wayForPayPaymentSettings);

			SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

			return View("~/Plugins/Payments.WayForPay/Views/PaymentWayForPay/Configure.cshtml", model);
		}

		[ChildActionOnly]
		public ActionResult PaymentInfo()
		{
			return View("~/Plugins/Payments.WayForPay/Views/PaymentWayForPay/PaymentInfo.cshtml");
		}

		[ChildActionOnly]
		public ActionResult PublicInfo(string widgetZone, object additionalData = null)
		{
			var model = new PublicInfoModel();

			OrderModel orderModel = additionalData as OrderModel;
			if (orderModel != null)
			{
				var order = _orderService.GetOrderById(orderModel.Id);
				model.OrderId = orderModel.Id;
				model.ShowInvoiceButton = order.PaymentMethodSystemName == "Payments.WayForPay"
					&& order.PaymentStatusId != (int)PaymentStatus.Paid;
			}

			return View("~/Plugins/Payments.WayForPay/Views/PaymentWayForPay/PublicInfo.cshtml", model);
		}

		[HttpPost]
		[AdminAuthorize]
		[ChildActionOnly]
		public ActionResult PublicInfo(ConfigurationModel model)
		{
			if (!ModelState.IsValid)
			{
				return PublicInfo("");
			}

			return View("~/Plugins/Payments.WayForPay/Views/PaymentWayForPay/PublicInfo.cshtml", model);
		}

		[ValidateInput(false)]
		public ActionResult IPNHandler(FormCollection form)
		{
			var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.WayForPay") as WayForPayPaymentPlugin;

			if (processor == null ||
				!processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
				throw new NopException("WayForPay module cannot be loaded");

			var nopOrderIdStr = GetValue(form, "orderReference");
			int nopOrderId;
			int.TryParse(nopOrderIdStr, out nopOrderId);
			var order = _orderService.GetOrderById(nopOrderId);

			if (order == null)
			{
				return RedirectToAction("Index", "Home", new { area = "" });
			}

			//var reasonCode = GetValue(form, "reasonCode");
			//if (string.IsNullOrEmpty(reasonCode))
			//{
			//	//TODO send invoice
			//	return RedirectToAction("Index", "Home", new { area = "" });
			//}

			WriteResponseNote(order, form);

			if (!IsPaymentValid(form))
			{
				WriteOrderNote(order, WayForPayConstants.PaymentMethodPrefix + " Not valid payment");
				return RedirectToRoute("OrderDetails", new { orderId = order.Id });
			}

			if (!IsValidSignature(form))
			{
				WriteOrderNote(order, WayForPayConstants.PaymentMethodPrefix + " Not valid signature");
				return RedirectToRoute("OrderDetails", new { orderId = order.Id });
			}

			var newPaymentStatus = PaymentStatus.Pending;

			if (IsOrderApproved(form))
			{
				newPaymentStatus = PaymentStatus.Paid;
			}

			var sb = new StringBuilder();
			sb.AppendLine(WayForPayConstants.PaymentMethodPrefix);
			sb.AppendLine("New payment status: " + newPaymentStatus);

			WriteOrderNote(order, sb.ToString());

			if (newPaymentStatus == PaymentStatus.Paid && _orderProcessingService.CanMarkOrderAsPaid(order))
			{
				_orderProcessingService.MarkOrderAsPaid(order);
				return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
			}

			return RedirectToRoute("OrderDetails", new { orderId = order.Id });
		}

		private string GetValue(FormCollection form, string key)
		{
			return form.AllKeys.Any(k => k.Equals(key, StringComparison.InvariantCultureIgnoreCase)) ? form[key] : _webHelper.QueryString<string>(key);
		}

		private bool IsPaymentValid(FormCollection form)
		{
			var merchantSignature = GetValue(form, "merchantSignature");
			var reasonCode = GetValue(form, "reasonCode");

			return !string.IsNullOrEmpty(merchantSignature) &&
				!string.IsNullOrEmpty(reasonCode) &&
				reasonCode.Equals(WayForPayConstants.OkReasonCode, StringComparison.InvariantCultureIgnoreCase);
		}

		private bool IsValidSignature(FormCollection form)
		{
			var collection = form.AllKeys.ToDictionary(k => k, v => (object)form[v]);
			return _wayForPayService.IsValidSignature(collection, GetValue(form, "merchantSignature"));
		}

		private bool IsOrderApproved(FormCollection form)
		{
			var transactionStatus = GetValue(form, "transactionStatus");
			return transactionStatus.Equals(WayForPayConstants.OrderStatusApproved, StringComparison.InvariantCultureIgnoreCase);
		}

		private void WriteResponseNote(Order order, FormCollection form)
		{
			var sbDebug = new StringBuilder();
			sbDebug.AppendLine(WayForPayConstants.PaymentMethodPrefix);

			foreach (var key in form.AllKeys)
			{
				var value = form[key];
				sbDebug.AppendLine(key + ": " + value);
			}

			if (!form.HasKeys())
				sbDebug.AppendLine("url: " + _webHelper.GetThisPageUrl(true));

			WriteOrderNote(order, sbDebug.ToString());
		}

		private void WriteOrderNote(Order order, string note)
		{
			order.OrderNotes.Add(new OrderNote
			{
				Note = note,
				DisplayToCustomer = false,
				CreatedOnUtc = DateTime.UtcNow
			});

			_orderService.UpdateOrder(order);
		}
	}
}
