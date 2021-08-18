using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.Models
{
    [Serializable]
    public class Document
    {
        public Guid GUID { get; set; }
        public string SfId { get; set; }

        public string FileName { get; set; }
        public int FileCategory { get; set; }

        public virtual DocumentType DocumentTypeFkNav { get; set; }
        public virtual ICollection<DocumentAccess> DocumentAccessFkNav { get; set; }
        public virtual ICollection<DocumentVersion> DocumentVersionsFkNav { get; set; }
    }
}
