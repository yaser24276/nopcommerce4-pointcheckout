using Nop.Core.Configuration;
using Nop.Core.Domain.Customers;
using System.Collections.Generic;

namespace Nop.Plugin.Payments.PointCheckoutPay
{
    /// <summary>
    /// Represents settings of the PointCheckout payment plugin
    /// </summary>
    public class PointCheckoutPayPaymentSettings : ISettings
    {
        

        /// <summary>
        /// Gets or sets a value indicating whether to Api Key
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets Api Secret
        /// </summary>
        public string ApiSecret { get; set; }

        /// <summary>
        /// Gets or sets PDT Enviroment
        /// </summary>
        public string Enviroment { get; set; }

        


    }
}
