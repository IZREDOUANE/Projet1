using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;

namespace Azure.Api.Services
{
    public interface IJwtTokenValidatorService
    {
        /// <summary>
        /// Generates a public token for the user
        /// </summary>
        /// <param name="claims">the properties we want to include in the token</param>
        /// <returns>an access token</returns>
        public string GenerateJwtToken(JObject claims);

        /// <summary>
        /// Checks if the token is valid
        /// </summary>
        /// <param name="token">user access token</param>
        /// <returns>a validation results</returns>
        SecurityToken IsTokenValid(string token);

        /// <summary>
        /// Gets the properties of the token
        /// </summary>
        /// <param name="token">user access token</param>
        /// <returns>a object of properties from the token</returns>
        JObject ExtractClaimsFromToken(JwtSecurityToken token);
    }
}
