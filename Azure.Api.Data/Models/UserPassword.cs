using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.Models
{
    public class UserPassword
    {
        public int ID { get; set; }

        public string SaltKey { get; set; }
        
        public string HashedPassword { get; set; }

        public Guid? UserGUID { get; set; }

        public virtual User UserFkNav { get; set; }
    }
}
