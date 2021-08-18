using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs
{
    public class DocumentUploadDTO
    {
        [JsonProperty]
        public bool IsPublic { get; private set; }

        [JsonProperty]
        public string[] AuthorizedUsersSfId { get; private set; }

        [JsonProperty]
        public DocumentDTO Details { get; private set; }

        [JsonProperty]
        public string Content { get; private set; }
    }
}
