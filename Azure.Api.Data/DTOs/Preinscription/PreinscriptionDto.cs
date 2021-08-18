using System;
using Azure.Api.Data.DTOs.Account;

namespace Azure.Api.Data.DTOs.Preinscription
{
    public class PreinscriptionDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string LPCR_Statut__c { get; set; }
        public string LPCR_EnfantNom__c { get; set; }
        public string LPCR_EnfantPrenom__c { get; set; }
        public string LPCR_ContactEnfant__c { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public string Tech_LienFormulaire__c { get; set; }
        public AccountCrecheDTO LPCR_Creche__r { get; set; }
    }
}
