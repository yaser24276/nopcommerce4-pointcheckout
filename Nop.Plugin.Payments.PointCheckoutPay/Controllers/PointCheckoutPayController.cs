using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.PointCheckoutPay.Models;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Plugin.Payments.PointCheckoutPay;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Nop.Services.Customers;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections;

namespace Nop.Plugin.Payments.PointCheckoutPay.Controllers
{
    public class PaymentPointCheckoutPayController : BasePaymentController
    {
        #region Fields

        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IPermissionService _permissionService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly IStoreContext _storeContext;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly PaymentSettings _paymentSettings;
        private readonly PointCheckoutPayPaymentSettings _PointCheckoutPayPaymentSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly ICustomerService _customerService;
        private readonly PointCheckoutPayPaymentProcessor _PointCheckoutPayPaymentProcessor;
        #endregion

        #region Ctor

        public PaymentPointCheckoutPayController(
            ICustomerService customerService,
            IWorkContext workContext,
            IStoreService storeService, 
            ISettingService settingService, 
            IPaymentService paymentService, 
            IOrderService orderService, 
            IOrderProcessingService orderProcessingService,
            IPermissionService permissionService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IStoreContext storeContext,
            ILogger logger, 
            IWebHelper webHelper,
            PaymentSettings paymentSettings,
            PointCheckoutPayPaymentSettings pointCheckoutPayPaymentSettings,
            ShoppingCartSettings shoppingCartSettings,
            PointCheckoutPayPaymentProcessor pointCheckoutPayPaymentProcessor)
        {
            this._customerService = customerService;
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._permissionService = permissionService;
            this._genericAttributeService = genericAttributeService;
            this._localizationService = localizationService;
            this._storeContext = storeContext;
            this._logger = logger;
            this._webHelper = webHelper;
            this._paymentSettings = paymentSettings;
            this._PointCheckoutPayPaymentSettings = pointCheckoutPayPaymentSettings;
            this._shoppingCartSettings = shoppingCartSettings;
            this._PointCheckoutPayPaymentProcessor = pointCheckoutPayPaymentProcessor;
        }

        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var PointCheckoutPayPaymentSettings = _settingService.LoadSetting<PointCheckoutPayPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                ApiKey = PointCheckoutPayPaymentSettings.ApiKey,
                ApiSecret = PointCheckoutPayPaymentSettings.ApiSecret,
                Enviroment = PointCheckoutPayPaymentSettings.Enviroment,
                ActiveStoreScopeConfiguration = storeScope
            };
            if (storeScope > 0)
            {
                model.ApiKey_OverrideForStore = _settingService.SettingExists(PointCheckoutPayPaymentSettings, x => x.ApiKey, storeScope);
                model.ApiSecret_OverrideForStore = _settingService.SettingExists(PointCheckoutPayPaymentSettings, x => x.ApiSecret, storeScope);
                model.Enviroment_OverrideForStore = _settingService.SettingExists(PointCheckoutPayPaymentSettings, x => x.Enviroment, storeScope);
            }

            //prepare enviroment modes list 
            bool enableStaging = false;
            string staging =  System.Environment.GetEnvironmentVariable("pointcheckout-staging");
            if (staging !=null && staging.Equals("true"))
            {
                 enableStaging = true;
            }
            model.AvailableEnviromentModes.Add(new SelectListItem
            {
                Text = "Test",
                Value = "1",
            });
            model.AvailableEnviromentModes.Add(new SelectListItem
            {
                Text = "Live",
                Value = "3",
            });
            if (enableStaging)
            {
                model.AvailableEnviromentModes.Add(new SelectListItem
                {
                    Text = "Staging",
                    Value = "2",
                });
            }

            return View("~/Plugins/Payments.PointCheckoutPay/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [AdminAntiForgery]
        [Area(AreaNames.Admin)]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

           
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var PointCheckoutPayPaymentSettings = _settingService.LoadSetting<PointCheckoutPayPaymentSettings>(storeScope);

            //save settings
            PointCheckoutPayPaymentSettings.ApiKey = model.ApiKey;
            PointCheckoutPayPaymentSettings.ApiSecret = model.ApiSecret;
            PointCheckoutPayPaymentSettings.Enviroment = model.Enviroment;
            

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(PointCheckoutPayPaymentSettings, x => x.ApiKey, model.ApiKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(PointCheckoutPayPaymentSettings, x => x.ApiSecret, model.ApiSecret_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(PointCheckoutPayPaymentSettings, x => x.Enviroment, model.Enviroment_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        //this method would be called when success redirect url 
        public IActionResult Confirm()
        {
            //get order id and checkout id from request 
            var checkout = HttpContext.Request.Query.FirstOrDefault(x => x.Key.Equals("checkout"));
            var orderId = HttpContext.Request.Query.FirstOrDefault(y => y.Key.Equals("reference"));
            //get the order data
            var order = _orderService.GetOrderById(int.Parse(orderId.Value));
            if (order != null)
            {
                    if (_PointCheckoutPayPaymentProcessor.CheckPayment(checkout.Value))
                    {
                        order.OrderStatus = OrderStatus.Processing;
                        order.PaymentStatus = PaymentStatus.Paid;
                        _orderService.UpdateOrder(order);
                    }
                    else
                    {
                        order.OrderStatus = OrderStatus.Cancelled;
                        order.PaymentStatus = PaymentStatus.Voided;
                       _orderService.UpdateOrder(order);
                    }
                    return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
             }  
            //if no such order with the provided id redirect to home page 
            return RedirectToRoute("HomePage");
        }
               

            


        public IActionResult CancelOrder()
        {
            var orderId = HttpContext.Request.Query.FirstOrDefault(y => y.Key.Equals("reference"));
            var order = _orderService.GetOrderById(int.Parse(orderId.Value));

            if (order != null)
            {
                order.OrderStatus = OrderStatus.Cancelled;
                order.PaymentStatus = PaymentStatus.Voided;
                _orderService.UpdateOrder(order);
                return RedirectToRoute("OrderDetails", new { orderId = order.Id });
            }
            return RedirectToRoute("HomePage");
        }

        #endregion
    }
}