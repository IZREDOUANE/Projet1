using Azure.Api.Data.DTOs.Email;
using Azure.Api.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs
{
    public class EmailDocumentIdAttachmentDTO : EmailDTO
    {
        public int[] Docsid { get; set; }

        public IEnumerable<EmailDocumentDTO> DocList { get; set; }
    }
}
