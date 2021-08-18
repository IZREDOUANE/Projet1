using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs
{
    public class DocumentDTO
    {
        public Guid? GUID { get; set; }

        public string SfId { get; set; }

        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string FileCategory { get; set; }
        public int FileSize { get; set; }

        public string ModifiedBy { get; set; }
    }
}
