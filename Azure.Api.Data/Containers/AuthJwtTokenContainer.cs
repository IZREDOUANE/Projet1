using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Azure.Api.Data.Containers
{
    public class AuthJwtTokenContainer
    {
        public SigningCredentials Credentials { get; set; }

        public long LifeTime { get; set; } // in minutes
        
        public Claim[] Claims { get; set; }
    }
}
