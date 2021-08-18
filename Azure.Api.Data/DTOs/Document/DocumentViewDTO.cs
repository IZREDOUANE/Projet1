using System;
using System.Collections.Generic;

namespace Azure.Api.Data.DTOs
{
    public class DocumentViewDTO
    {
        public Guid GUID { get; set; }

        public string[] Owners { get; set; }

        public string FileName { get; set; }
        public string FileCategory { get; set; }

        public IEnumerable<DocumentVersionDTO> Versions { get; set; }
    }
}
