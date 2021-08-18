using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.Models
{
    public class UserAccess
    {
        public Guid? UserGUID { get; set; }
        public int PlatformID { get; set; }

        public virtual User UserFkNav { get; set; }
        public virtual Platform PlatformFkNav { get; set; }
    }
}
