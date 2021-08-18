using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.DTOs
{
    public class AuthenticationDTO
    {
        public string Login { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Token { get; set; }
    }
}
