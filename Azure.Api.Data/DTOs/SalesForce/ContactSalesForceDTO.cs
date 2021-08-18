using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs.SalesForce
{
    public class ContactSalesForceDTO
    {
        public string id { get; set; }
        public string Lastname { get; set; }
        public string FirstName { get; set; }

        public string AccountId { get; set; }
    }
}
