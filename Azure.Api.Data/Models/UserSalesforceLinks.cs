using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.Models
{
    public class UserSalesforceLink
    {
        public Guid UserGUID { get; set; }
        public string SalesforceAccountId { get; set; }

        public virtual User UserNavFk { get; set; }
    }
}
