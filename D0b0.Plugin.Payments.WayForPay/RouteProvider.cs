using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace D0b0.Plugin.Payments.WayForPay
{
	public partial class RouteProvider : IRouteProvider
	{
		#region Methods

		public void RegisterRoutes(RouteCollection routes)
		{
			routes.MapRoute("Plugin.Payments.WayForPay.IPNHandler",
				"Plugins/PaymentWayForPay/IPNHandler",
				new { controller = "PaymentWayForPay", action = "IPNHandler" },
				new[] { "D0b0.Plugin.Payments.WayForPay.Controllers" }
			);
		}

		#endregion

		#region Properties

		public int Priority => 0;

		#endregion
	}
}
