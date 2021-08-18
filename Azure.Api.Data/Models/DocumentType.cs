using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Azure.Api.Data.Models
{
    public class DocumentType
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public bool IsUnique { get; set; }
        public bool IsRequired { get; set; }

        public virtual ICollection<Document> Documents { get; set; }
    }
}
