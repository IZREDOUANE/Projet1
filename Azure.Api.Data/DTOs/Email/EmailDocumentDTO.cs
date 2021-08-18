using MimeTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs.Email
{
    public class EmailDocumentDTO
    {
        public string Name { get; set; }
        public string FileType { get; set; }
        public byte[] Content { get; set; }
    }
}
