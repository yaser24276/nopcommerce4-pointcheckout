﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.PointCheckoutPay
{
    class PointCheckoutRequest
    {
        public string referenceId;
        public decimal grandtotal;
        public decimal subtotal;
        public decimal shipping;
        public decimal tax;
        public decimal discount;
        public string currency;
        public string successUrl;
        public string failureUrl;
        public List<RquestItem> items;
        public OrderCustomer customer;       
    }

    public class RquestItem
    {
        public string name;
        public string sku;
        public decimal quantity;
        public decimal total;
    }
    public class OrderCustomer
    {
        public string firstName;
        public string lastName;
        public string email;
        public string phone;
        public Address billingAddress;
        public Address shippingAddress;
    }

    public class Address
    {
        public string name;
        public string address1;
        public string address2;
        public string city;
        public string state;
        public string zip;
        public string country;
    }


}
