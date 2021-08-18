using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs
{
    public class EmailDocumentBinaryAttachmentDTO : EmailDTO
    {
        public BinaryDocumentDTO[] documents;
    }
}
