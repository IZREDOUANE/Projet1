using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.Models
{
    public class Platform
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string DomainName { get; set; }

        public virtual ICollection<UserAccess> UserAccessFkNav { get; set; }
        public virtual ICollection<AdminAccess> AdminAccessFkNav { get; set; }
    }
}
