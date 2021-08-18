using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs.Contact
{
    public class ContactLpcrDTO
    {
        public string Type { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Email { get; set; }
        public string Phone { get; set; }
        public string MobilePhone { get; set; }
    }
}
