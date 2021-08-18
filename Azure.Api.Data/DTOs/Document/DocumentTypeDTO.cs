using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs.Document
{
    public class DocumentTypeDTO
    {
        public string Type { get; set; }
        public bool IsUnique { get; set; }

        public bool IsRequired { get; set; }
    }
}
