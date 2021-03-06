﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.PointCheckoutPay
{
    public class PointCheckoutResponse
    {
        public bool success;
        public decimal elapsed;
        public Result result;
        public string error;
           
    }

    public class Result
    {
        public string checkoutId;
        public string merchantId;
        public string merchantName;
        public string referenceId;
        public string currency;
        public decimal grandtotal;
        public string status;
        public string checkoutKey;
        public decimal cod;
        public string checkoutDisplayId;

    }
   

}
