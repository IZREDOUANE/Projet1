using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs.Account
{
    public class ShippingAddressDTO
    {
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string Street { get; set; }
        public string Country { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}