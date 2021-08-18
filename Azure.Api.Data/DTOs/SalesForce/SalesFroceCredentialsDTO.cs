using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs.SalesForce
{
    public class SalesFroceCredentialsDTO
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        public string Username { get; set; }
        public string SecurityToken { get; set; }
        public string Password { get; set; }
        public string EndPointUrl { get; set; }
    }
}
