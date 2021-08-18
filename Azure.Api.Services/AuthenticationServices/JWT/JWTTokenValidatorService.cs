using Azure.Api.Data.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace Azure.Api.Services
{
    public class JwtTokenValidatorService : IJwtTokenValidatorService
    {
        private readonly IConfiguration _config;

        public JwtTokenValidatorService(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #region Security key methods

        private SigningCredentials GetJwtSecurityCredentials()
        {
            return new SigningCredentials(GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256Signature);
        }

        private SecurityKey GetSymmetricSecurityKey()
        {
            var tokenKey = _config.GetSection("APISecurity").GetValue<string>("TokenKey");

            var key = Encoding.ASCII.GetBytes(tokenKey); 
            return new SymmetricSecurityKey(key);
        }

        #endregion

        #region Token generation

        public string GenerateJwtToken(JObject claims)
        {
            var handler = new JwtSecurityTokenHandler();

            var container = GenerateTokenContainer(claims);

            var descript = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(container.Claims),
                Expires = DateTime.UtcNow.AddMinutes(container.LifeTime),
                SigningCredentials = container.Credentials
            };

            var token = handler.CreateToken(descript);

            return handler.WriteToken(token);
        }

        private AuthJwtTokenContainer GenerateTokenContainer(JObject claims) // all values will be converted to string
        {
            if (claims == null)
            {
                throw new ArgumentNullException("claims object need to be initialized");
            }

            var props = claims.Properties().ToList();
            var nbProps = props.Count;

            if (nbProps == 0)
            {
                throw new ArgumentException("You need at least one claim");
            }

            var container = new AuthJwtTokenContainer
            {
                Credentials = GetJwtSecurityCredentials(), 
                LifeTime = _config.GetSection("APISecurity").GetValue<long>("TokenMinutesLifeTime"),
                Claims = new Claim[nbProps]
            };

            for (int i = 0; i < nbProps; ++i)
            {
                var prop = props[i];
                container.Claims[i] = new Claim(prop.Name, prop.Value.ToString());
            }

            return container;
        }
        #endregion

        #region Token validation

        public SecurityToken IsTokenValid(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            var validationParams = GetValidationParameters();

            var handler = new JwtSecurityTokenHandler();
            try
            {
                handler.ValidateToken(token, validationParams, out SecurityToken validatedToken);
                return validatedToken;
            }

            catch (Exception)
            {
                return null;
            }
        }

        private TokenValidationParameters GetValidationParameters()
        {
            return new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = GetSymmetricSecurityKey(),

                ClockSkew = TimeSpan.Zero
            };
        }

        #endregion

        public JObject ExtractClaimsFromToken(JwtSecurityToken token)
        {
            var extractedProps = new Dictionary<string, string>();

            foreach (var claim in token.Claims)
            {
                extractedProps.Add(claim.Type, claim.Value);
            }

            return JObject.FromObject(extractedProps);
        }
    }
}
