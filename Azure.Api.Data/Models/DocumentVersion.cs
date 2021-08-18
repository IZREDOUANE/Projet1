using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.Models
{
    public class DocumentVersion
    {
        public Guid BlobGUID { get; set; }
        public Guid DocumentGUID { get; set; }

        public string FileExtension { get; set; }
        public long FileSize { get; set; }
        
        public DateTime Date { get; set; }
        public string ModifiedBy { get; set; }

        public string CheckSum { get; set; }

        public virtual Document DocumentFkNav { get; set; }
    }
}
