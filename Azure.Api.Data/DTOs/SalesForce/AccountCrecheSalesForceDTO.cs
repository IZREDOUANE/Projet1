using Azure.Api.Data.DTOs.Account;
using Azure.Api.Data.DTOs.SalesForce;
using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs.SalesForce
{
    public class AccountCrecheSalesForceDTO
    {
        public string Name { get; set; }

        public string Id { get; set; }

        public string ParentId { get; set; }


        public string Email__c { get; set; }

        public string Lpcr_BerceauxCommercialisables__c { get; set; }

        public string LPCR_BerceauxDisponibles__c { get; set; }

        public string OwnerId { get; set; }

        public string Ownership { get; set; }

        public string Phone { get; set; }


        public string LPCR_TypeCreche__c { get; set; }

        public string LPCR_NombreEnfantsLundi__c { get; set; }

        public string LPCR_NombreEnfantsMardi__c { get; set; }

        public string LPCR_NombreEnfantsMercredi__c { get; set; }

        public string LPCR_NombreEnfantPrevuJeudi__c { get; set; }

        public string LPCR_NombreEnfantPrevuVendredi__c { get; set; }

        public string LPCR_AgrementBebes__c { get; set; }

        public string LPCR_AgrementMoyens__c { get; set; }

        public string LPCR_AgrementGrands__c { get; set; }

        public string LPCR_AgrementTotal__c { get; set; }

        public string LPCR_NombreOptions__c { get; set; }
        public ShippingAddressDTO ShippingAddress { get; set; }
        public ContactDirectriceSalesforceDTO LPCR_ContactDirectrice__r { get; set; }
        public InterlocuteurServiceFamilleSalesForceDTO LPCR_InterlocuteurServiceFamille__r { get; set; }

        public string LPCR_ResponsableServiceFamille__c { get; set; }
    }
}
