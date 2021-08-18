using Azure.Api.Data.DTOs.SalesForce;
using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs.Account
{
    public class AccountCrecheDTO
    {
        public string sfId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string BerceauxCommercialisable { get; set; }
        public string BerceauxDisponibles { get; set; }
        public string Phone { get; set; }
        public ShippingAddressDTO ShippingAddress { get; set; }
        public ContactDirectriceSalesforceDTO ContactDirectrice { get; set; }
        public InterlocuteurServiceFamilleSalesForceDTO ServiceFamille { get; set; }
    }
}