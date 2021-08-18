using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.Models
{
    public class DocumentAccess
    {
        public string AllowedSfId { get; set; }
        public Guid DocumentGUID { get; set; }

        public Document DocumentFkNav { get; set; }
    }
}
