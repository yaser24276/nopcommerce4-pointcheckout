﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.PointCheckoutPay
{
    public partial class RouteProvider : IRouteProvider
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="routeBuilder">Route builder</param>
        public void RegisterRoutes(IRouteBuilder routeBuilder)
        {
            //Confirm Payemnt success call
            routeBuilder.MapRoute("Plugin.Payments.PointCheckoutPay.Confirm", "Plugins/PaymentPointCheckoutPay/Confirm",
                 new { controller = "PaymentPointCheckoutPay", action = "Confirm", });


            //Cancel Payment when failed 
            routeBuilder.MapRoute("Plugin.Payments.PointCheckoutPay.CancelOrder", "Plugins/PaymentPointCheckoutPay/CancelOrder",
                 new { controller = "PaymentPointCheckoutPay", action = "CancelOrder" });
        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority
        {
            get { return -1; }
        }
    }
}
