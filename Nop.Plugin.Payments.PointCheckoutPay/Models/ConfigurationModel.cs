﻿using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Mvc.Models;
using System.Collections.Generic;

namespace Nop.Plugin.Payments.PointCheckoutPay.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public ConfigurationModel()
        {
            this.AvailableEnviromentModes = new List<SelectListItem>();
        }
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PointCheckoutPay.Fields.ApiKey")]
        public string ApiKey { get; set; }
        public bool ApiKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PointCheckoutPay.Fields.ApiSecret")]
        public string ApiSecret { get; set; }
        public bool ApiSecret_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PointCheckoutPay.Fields.Enviroment")]
        public string Enviroment { get; set; }
        public List<SelectListItem> AvailableEnviromentModes { get; set; }
        public bool Enviroment_OverrideForStore { get; set; }
        
    }
}