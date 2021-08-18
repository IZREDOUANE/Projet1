using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs.SalesForce
{
    public class ContactParentEnfantSalesForceDTO
    {
        public string Id { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Phone { get; set; }

        public string MobilePhone { get; set; }

        public DateTime? Birthdate { get; set; }

        public string RecordTypeId { get; set; }

        public string AccountId { get; set; }

        public bool LPCR_AutoriteParentale__c { get; set; }

        public RecordTypeDTO RecordType { get; set; }
    }
}