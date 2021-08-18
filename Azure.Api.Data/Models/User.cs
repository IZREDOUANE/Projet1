using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.Models
{
    public class User
    {
        public Guid? GUID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Login { get; set; }
        public bool ResetPassword { get; set; }
        public bool IsActive { get; set; }
        public string PhoneNumber { get; set; }
        public Guid? ConfirmEmailToken { get; set; }

        public virtual ICollection<UserAccess> UserAccessFkNav { get; set; } 
        public virtual UserPassword UserPasswordFkNav { get; set; }
        public virtual ICollection<UserSalesforceLink> UserSfLinkFkNav { get; set; }
    }
}
