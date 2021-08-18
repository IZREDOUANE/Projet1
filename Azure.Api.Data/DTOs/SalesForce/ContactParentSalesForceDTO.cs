using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs.SalesForce
{
    public class ContactParentSalesForceDTO: ContactSalesForceDTO
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string LastName { get; set; }
        public string RecordTypeId { get; set; }
        public string MailingStreet { get; set; }
        public string MailingCity { get; set; }
        public string MailingPostalCode { get; set; }
        public string HomePhone { get; set; }
        public string MobilePhone { get; set; }
        public int LPCR_NombreEnfantsCharge__c { get; set; }
        public string LPCR_SituationFamiliale__c { get; set; }
        public string LPCR_AutoriteParentale__c { get; set; }

        //num_caf_msa
        //nbre_enfant_handicape
        //nbre_jours_conges
    }
}
