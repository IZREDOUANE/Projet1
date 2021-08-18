using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs
{
    public class BinaryDocumentDTO
    {
        public byte[] Content { get; set; }
        public string FileType { get; set; }
        public string Name { get; set; }
    }
}
