using Azure.Api.Data.DTOs.Account;
using Salesforce.Common.Models.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs.SalesForce
{
    public class AccountSalesForceDTO
    {
        public string id { get; set; }
        public string BillingAddress { get; set; }
        public ShippingAddressDTO ShippingAddress { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email__c { get; set; }
        public string RecordTypeId { get; set; }
          
        public string LPCR_NombreEnfantsCharge__c { get; set; }
        public string LPCR_NumeroAllocataire__c { get; set; }
        public bool LPCR_Handicap__c { get; set; }
        public string LPCR_Garde__c { get; set; }

        public QueryResult<ContactParentEnfantSalesForceDTO> Contacts { get; set; }
    }
}
