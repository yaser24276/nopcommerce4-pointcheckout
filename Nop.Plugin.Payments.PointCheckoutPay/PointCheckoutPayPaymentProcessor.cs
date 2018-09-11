using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Plugins;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Tax;

namespace Nop.Plugin.Payments.PointCheckoutPay
{
    /// <summary>
    /// PointCheckoutPay payment processor
    /// </summary>
    public class PointCheckoutPayPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICurrencyService _currencyService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly ISettingService _settingService;
        private readonly ITaxService _taxService;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly IOrderService _orderService;
        private readonly PointCheckoutPayPaymentSettings _PointCheckoutPayPaymentSettings;
        private static  PointCheckoutPayPaymentProcessor instance;

        #endregion

        #region Ctor

        public PointCheckoutPayPaymentProcessor(CurrencySettings currencySettings,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICurrencyService currencyService,
            IGenericAttributeService genericAttributeService,
            IHttpContextAccessor httpContextAccessor,
            ILocalizationService localizationService,
            IOrderTotalCalculationService orderTotalCalculationService,
            ISettingService settingService,
            ITaxService taxService,
            IWebHelper webHelper,
            IWorkContext workContext,
            IOrderService orderService,
            PointCheckoutPayPaymentSettings PointCheckoutPayPaymentSettings)
        {
            this._currencySettings = currencySettings;
            this._checkoutAttributeParser = checkoutAttributeParser;
            this._currencyService = currencyService;
            this._genericAttributeService = genericAttributeService;
            this._httpContextAccessor = httpContextAccessor;
            this._localizationService = localizationService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._settingService = settingService;
            this._taxService = taxService;
            this._webHelper = webHelper;
            this._workContext = workContext;
            this._orderService = orderService;
            this._PointCheckoutPayPaymentSettings = PointCheckoutPayPaymentSettings;
            instance = this;
        }

        public static PointCheckoutPayPaymentProcessor GetInstance()
        {
            return instance;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets PointCheckout URL
        /// </summary>
        /// <returns></returns>
        public string GetPointCheckoutBaseUrl()
        {
            if(_PointCheckoutPayPaymentSettings.Enviroment == "1")
            {
                return "https://pay.test.pointcheckout.com";
            }else if(_PointCheckoutPayPaymentSettings.Enviroment == "2")
            {
                return "https://pay.staging.pointcheckout.com";
            }
            return "https://pay.pointcheckout.com";
        }

        /// <summary>
        /// Gets APi PointCheckout URL
        /// </summary>
        /// <returns></returns>
        public string GetPointCheckoutApiUrl()
        {
            if (_PointCheckoutPayPaymentSettings.Enviroment == "1")
            {
                return "https://pay.test.pointcheckout.com/api/v1.0/checkout";
            }
            else if (_PointCheckoutPayPaymentSettings.Enviroment == "2")
            {
                return "https://pay.staging.pointcheckout.com/api/v1.0/checkout";
            }
            return "https://pay.pointcheckout.com/api/v1.0/checkout";
        }

       


        /// <summary>
        /// Create common query parameters for the request
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Created query parameters</returns>
        private PointCheckoutRequest CreateQueryParameters(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //get store location
            var storeLocation = _webHelper.GetStoreLocation();

            //create query parameters
            
            Address billingAddress = new Address();
            Address shippingAddress = new Address();
            
            List<RquestItem> items = new List<RquestItem>();
         
            ICollection<OrderItem> orderItems = postProcessPaymentRequest.Order.OrderItems;
            foreach (OrderItem orderItem in orderItems)
            {
                RquestItem item = new RquestItem()
                {

                    name = orderItem.Product.Name,
                    sku = orderItem.Product.Sku,
                    quantity = orderItem.Quantity,
                    total = orderItem.PriceExclTax * orderItem.Quantity
                };
                items.Add(item);
            }
            billingAddress.name = postProcessPaymentRequest.Order.BillingAddress.FirstName + postProcessPaymentRequest.Order.Customer.BillingAddress.LastName;
            billingAddress.address1 = postProcessPaymentRequest.Order.BillingAddress.Address1;
            billingAddress.address2 = postProcessPaymentRequest.Order.BillingAddress.Address2;
            billingAddress.city = postProcessPaymentRequest.Order.BillingAddress.City;
            billingAddress.state = postProcessPaymentRequest.Order.BillingAddress.StateProvince!=null? postProcessPaymentRequest.Order.BillingAddress.StateProvince.Name:"";
            billingAddress.zip = postProcessPaymentRequest.Order.BillingAddress.ZipPostalCode;
            billingAddress.country = postProcessPaymentRequest.Order.BillingAddress.Country.Name;
            if (!(postProcessPaymentRequest.Order.ShippingStatus == ShippingStatus.ShippingNotRequired)) {
                shippingAddress.name = postProcessPaymentRequest.Order.ShippingAddress.FirstName + postProcessPaymentRequest.Order.Customer.ShippingAddress.LastName;
                shippingAddress.address1 = postProcessPaymentRequest.Order.ShippingAddress.Address1;
                shippingAddress.address2 = postProcessPaymentRequest.Order.ShippingAddress.Address2;
                shippingAddress.city = postProcessPaymentRequest.Order.ShippingAddress.City;
                shippingAddress.state = postProcessPaymentRequest.Order.ShippingAddress.StateProvince != null ? postProcessPaymentRequest.Order.BillingAddress.StateProvince.Name : "";
                shippingAddress.zip = postProcessPaymentRequest.Order.ShippingAddress.ZipPostalCode;
                shippingAddress.country = postProcessPaymentRequest.Order.ShippingAddress.Country.Name;
            }
            OrderCustomer customer = new OrderCustomer()
            {
                firstName = postProcessPaymentRequest.Order.BillingAddress.FirstName,
                lastName = postProcessPaymentRequest.Order.BillingAddress.LastName,
                email = postProcessPaymentRequest.Order.BillingAddress.Email,
                phone = postProcessPaymentRequest.Order.BillingAddress.PhoneNumber,
                billingAddress = billingAddress,
                shippingAddress = shippingAddress
            };

            PointCheckoutRequest request = new PointCheckoutRequest()
            {
                referenceId = postProcessPaymentRequest.Order.Id.ToString(),
                grandtotal = postProcessPaymentRequest.Order.OrderTotal,
                subtotal = postProcessPaymentRequest.Order.OrderSubtotalExclTax,
                tax = postProcessPaymentRequest.Order.OrderTax,
                discount = postProcessPaymentRequest.Order.OrderDiscount,
                shipping = postProcessPaymentRequest.Order.OrderShippingInclTax,
                successUrl = _webHelper.GetStoreLocation() + "Plugins/PaymentPointCheckoutPay/Confirm",
                failureUrl = _webHelper.GetStoreLocation() + "Plugins/PaymentPointCheckoutPay/CancelOrder",
                currency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)?.CurrencyCode,
                items = items,
                customer = customer
        };
            return request;

        }

       

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult();
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //create common query parameters for the request
            var queryParameters = CreateQueryParameters(postProcessPaymentRequest);
            string jsonString = JsonConvert.SerializeObject(queryParameters);
            var url =GetPointCheckoutBaseUrl() + "/api/v1.0/checkout";
            HttpClient pointCheckoutClient = getPointcheckoutHttpClient(url);       
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonString, Encoding.UTF8, "application/json")
            };
            try
            {
                pointCheckoutClient.CancelPendingRequests();
                HttpResponseMessage response = pointCheckoutClient.SendAsync(request).Result;
                string responseString =  response.Content.ReadAsStringAsync().Result;
                PointCheckoutResponse responseObject = JsonConvert.DeserializeObject<PointCheckoutResponse>(responseString);
                if (responseObject.success)
                {
                    string redirectUrl = GetPointCheckoutBaseUrl() + "/checkout/" + responseObject.result.checkoutKey;
                    var order = _orderService.GetOrderById(int.Parse(responseObject.result.referenceId));
                    //add a note
                    order.OrderNotes.Add(new OrderNote()
                    {
                        Note = getOrderHistoryCommentMessage(responseObject.result.checkoutId, responseObject.result.status, responseObject.result.currency, 0),
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                    _orderService.UpdateOrder(order);
                    _httpContextAccessor.HttpContext.Response.Redirect(redirectUrl);
                    return;
                }
                else
                { 
                   throw new NopException("ERROR: "+responseObject.error);
                }
            }catch (Exception ex)
            {
                ex.GetBaseException();
                _httpContextAccessor.HttpContext.Response.Redirect(_webHelper.GetStoreLocation());
            }
        }


        /// <summary>
        /// checkPayment (check if payment is success and paid )
        /// </summary>
        /// <param name="checkout">checkout id provided by pointcheckout redirect call </param>
        public PointCheckoutResponse CheckPayment(string checkout)
        {
            //create common query parameters for the request
            var url = GetPointCheckoutBaseUrl() + "/api/v1.0/checkout/"+checkout;
            HttpClient pointCheckoutClient = getPointcheckoutHttpClient(url);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            try
            {
                pointCheckoutClient.CancelPendingRequests();
                HttpResponseMessage response = pointCheckoutClient.SendAsync(request).Result;
                response.EnsureSuccessStatusCode();
                string responseString = response.Content.ReadAsStringAsync().Result;
                PointCheckoutResponse responseObject = JsonConvert.DeserializeObject<PointCheckoutResponse>(responseString);
                return responseObject;
               
            }
            catch (Exception ex)
            {
                ex.GetBaseException();
                return null;
            }
        }



        /// <summary>
        /// prepare pointcheckout httpClient settings 
        /// </summary>
        /// <param name="url"> pointcheckout base url </param>
        private HttpClient getPointcheckoutHttpClient(string url)
        {
            HttpClient httpClient = new HttpClient()
            {
                BaseAddress = new Uri(url)
            };

            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("Api-Key", _PointCheckoutPayPaymentSettings.ApiKey);
            httpClient.DefaultRequestHeaders.Add("Api-Secret", _PointCheckoutPayPaymentSettings.ApiSecret);
            return httpClient;

        }


        private string getOrderHistoryCommentMessage(string checkoutId, string status, string currency, decimal cod)
        {

            string message = "PointCheckout Status: " + status + "\n" + "PointCheckout Transaction ID: " + checkoutId + "\n";
            if (cod > 0)
            {
                message += "[NOTICE] COD Amount: " + cod + " " + currency + "\n";
            }
            message += "Transaction Url: " + getAdminUrl() + "/merchant/transactions/" + checkoutId + "/read";
            return message;
        }

        private string getAdminUrl()
        {
            if (_PointCheckoutPayPaymentSettings.Enviroment == "1")
            {
                return "https://admin.test.pointcheckout.com";
            }
            else if (_PointCheckoutPayPaymentSettings.Enviroment == "2")
            {
                return "https://admin.staging.pointcheckout.com";
            }
            return "https://admin.pointcheckout.com";
        }


        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
         
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
                return false;
        }

       

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            return new CapturePaymentResult { Errors = new[] { "Capture method not supported" } };
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            return new RefundPaymentResult { Errors = new[] { "Refund method not supported" } };
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            return new VoidPaymentResult { Errors = new[] { "Void method not supported" } };
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
           // if (order == null)
            //    throw new ArgumentNullException(nameof(order));
            
            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
           // if ((DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds < 5)
             //   return false;

            //AS FOR NOW WE DON'T WANT TO ALLOW REPOSTING THE PAYMENT

            return false;
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>List of validating errors</returns>
        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            return new List<string>();
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>Payment info holder</returns>
        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            return new ProcessPaymentRequest();
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentPointCheckoutPay/Configure";
        }

        /// <summary>
        /// Gets a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <param name="viewComponentName">View component name</param>
        public void GetPublicViewComponent(out string viewComponentName)
        {
            viewComponentName = "PaymentPointCheckoutPay";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            //settings
            _settingService.SaveSetting(new PointCheckoutPayPaymentSettings());
            
            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PointCheckoutPay.Fields.ApiKey", "Api Key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PointCheckoutPay.Fields.ApiKey.Hint", "Enter your Api Key provided by PointCheckout.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PointCheckoutPay.Fields.ApiSecret", "Api Secret");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PointCheckoutPay.Fields.ApiSecret.Hint", "Enter your Api Secret provided by PointCheckout.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PointCheckoutPay.Fields.Enviroment", "Mode");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PointCheckoutPay.Fields.Enviroment.Hint", "select the payment method mode.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PointCheckoutPay.PaymentMethodDescription", "use your reward points and miles to pay for your shopping cart");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PointCheckoutPay.Instructions", "To start accepting pointcheckout payments, please <a href=\"https://www.pointcheckout.com/home/merchant/contact\"> contact PointCheckout </a> to get your access credintials which is used in all communication with PointCheckout.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PointCheckoutPay.Fields.PaymentInfo", "<img src=" + "https://www.pointcheckout.com/image/logo.png" + " width=" + "250px" + "/><br/>You will be redirected to PointCheckout site to complete payment.");

            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<PointCheckoutPayPaymentSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.PointCheckoutPay.Fields.ApiKey");
            this.DeletePluginLocaleResource("Plugins.Payments.PointCheckoutPay.Fields.ApiKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.PointCheckoutPay.Fields.ApiSecret");
            this.DeletePluginLocaleResource("Plugins.Payments.PointCheckoutPay.Fields.ApiSecret.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.PointCheckoutPay.Fields.Enviroment");
            this.DeletePluginLocaleResource("Plugins.Payments.PointCheckoutPay.Fields.Enviroment.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.PointCheckoutPay.Fields.PaymentInfo");
            this.DeletePluginLocaleResource("Plugins.Payments.PointCheckoutPay.Instructions");
            this.DeletePluginLocaleResource("Plugins.Payments.PointCheckoutPay.PaymentMethodDescription");

            base.Uninstall();
        }

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return 0;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get { return RecurringPaymentType.NotSupported; }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get { return PaymentMethodType.Redirection; }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription
        {
            //return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
            //for example, for a redirection payment method, description may be like this: "You will be redirected to PointCheckout site to complete the payment"
            get { return _localizationService.GetResource("Plugins.Payments.PointCheckoutPay.PaymentMethodDescription"); }
        }

        #endregion
    }
}
