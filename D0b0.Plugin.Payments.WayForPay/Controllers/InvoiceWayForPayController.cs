using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using D0b0.Plugin.Payments.WayForPay.Domain;
using D0b0.Plugin.Payments.WayForPay.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;

namespace D0b0.Plugin.Payments.WayForPay.Controllers
{
	public class InvoiceWayForPayController : BaseController
	{
		private readonly WayForPayPaymentSettings _wayForPayPaymentSettings;
		private readonly PaymentSettings _paymentSettings;
		private readonly HttpContextBase _httpContext;
		private readonly IPaymentService _paymentService;
		private readonly IOrderService _orderService;
		private readonly IOrderProcessingService _orderProcessingService;
		private readonly IWebHelper _webHelper;
		private readonly IWayForPayService _wayForPayService;

		public InvoiceWayForPayController(
			WayForPayPaymentSettings wayForPayPaymentSettings,
			PaymentSettings paymentSettings,
			HttpContextBase httpContext,
			IPaymentService paymentService,
			IOrderService orderService,
			IOrderProcessingService orderProcessingService,
			IWebHelper webHelper,
			IWayForPayService wayForPayService)
		{
			_paymentService = paymentService;
			_orderService = orderService;
			_httpContext = httpContext;
			_orderProcessingService = orderProcessingService;
			_wayForPayPaymentSettings = wayForPayPaymentSettings;
			_webHelper = webHelper;
			_paymentSettings = paymentSettings;
			_wayForPayService = wayForPayService;
		}

		[HttpPost]
		[ValidateInput(false)]
		public ActionResult IPN()
		{
			var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.WayForPay") as WayForPayPaymentPlugin;

			if (processor == null ||
				!processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
				throw new NopException("WayForPay module cannot be loaded");

			var data = JsonConvert.DeserializeObject<InvoiceStatus>(Request.Form[0]);

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

			WriteOrderNote(order, Request.Form[0]);

			if (!IsPaymentValid(data.ReasonCode, data.MerchantSignature))
			{
				WriteOrderNote(order, WayForPayConstants.PaymentMethodPrefix + " Not valid payment");
				return RedirectToRoute("OrderDetails", new { orderId = order.Id });
			}

			var collection = JsonConvert.DeserializeObject<IDictionary<string, object>>(Request.Form[0]);
			if (!_wayForPayService.IsValidSignature(collection, data.MerchantSignature))
			{
				WriteOrderNote(order, WayForPayConstants.PaymentMethodPrefix + " Not valid signature");
				return RedirectToRoute("OrderDetails", new { orderId = order.Id });
			}

			var newPaymentStatus = PaymentStatus.Pending;
			if (IsOrderApproved(data.TransactionStatus))
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
			}

			WriteAck(data.OrderReference);
			return new HttpStatusCodeResult(HttpStatusCode.OK);
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

		private bool IsPaymentValid(string reasonCode, string merchantSignature)
		{
			return !string.IsNullOrEmpty(merchantSignature) &&
				!string.IsNullOrEmpty(reasonCode) &&
				reasonCode.Equals(WayForPayConstants.OkReasonCode, StringComparison.InvariantCultureIgnoreCase);
		}

		private bool IsOrderApproved(string transactionStatus)
		{
			return transactionStatus.Equals(WayForPayConstants.OrderStatusApproved, StringComparison.InvariantCultureIgnoreCase);
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
