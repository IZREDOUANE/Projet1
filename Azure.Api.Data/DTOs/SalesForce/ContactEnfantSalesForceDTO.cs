using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs.SalesForce
{
    public class ContactEnfantSalesForceDTO: ContactSalesForceDTO
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string LastName { get; set; }
        public string RecordTypeId { get; set; }
        public DateTime Birthdate { get; set; }
    }
}
