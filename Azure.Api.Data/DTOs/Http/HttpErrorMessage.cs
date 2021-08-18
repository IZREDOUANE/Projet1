using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs
{
    public class HttpErrorMessage : IHttpMessage
    {
        public string ErrorMessage { get; private set; }

        public HttpErrorMessage(string message)
        {
            ErrorMessage = message;
        }
    }
}
