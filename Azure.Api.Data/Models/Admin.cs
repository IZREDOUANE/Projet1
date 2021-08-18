using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.Models
{
    public class Admin
    {
        public int ID { get; set; }
        public string ADGUID { get; set; }

        public virtual ICollection<AdminAccess> AdminAccessFkNav { get; set; }
    }
}
