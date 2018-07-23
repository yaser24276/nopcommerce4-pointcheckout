using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.PointCheckoutPay.Components
{
    [ViewComponent(Name = "PaymentPointCheckoutPay")]
    public class PaymentPointCheckoutPayViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Payments.PointCheckoutPay/Views/PaymentInfo.cshtml");
        }
    }
}
