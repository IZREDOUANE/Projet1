using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs
{
    public class HttpSuccessMessage : IHttpMessage
    {
        public string SuccessMessage { get; private set; }

        public HttpSuccessMessage(string message)
        {
            SuccessMessage = message;
        }
    }
}
