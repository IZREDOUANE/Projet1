using System;

namespace Azure.Api.Data.DTOs
{
    public class DocumentVersionDTO
    {
        public Guid BlobGUID { get; set; }

        public string FileExtension { get; set; }
        public long FileSize { get; set; }

        public DateTime Date { get; set; }

        public string ModifiedBy { get; set; }
    }
}
