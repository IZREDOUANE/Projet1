using Azure.Api.Data.DTOs;
using Azure.Api.Data.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Azure.Api.Services
{
    public class SSOAuthenticationService : ISSOAuthenticationService
    {
        private readonly IJwtTokenValidatorService _jwtvService;
        private readonly IAdminService _adminService;

        public SSOAuthenticationService(IAdminService adminService, IJwtTokenValidatorService jwtvService)
        {
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
            _jwtvService = jwtvService ?? throw new ArgumentNullException(nameof(jwtvService));
        }

        public async Task<AdminAthenticationDTO> GetUserBySSOLogin(ClaimsPrincipal principal, string appDomainName)
        {
            try
            {
                string guid = principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

                if (guid == null)
                {
                    throw new Exception();
                }

                AdminAccess adminAccess = await _adminService.GetAdminAccess(guid, appDomainName);
                if (adminAccess == null)
                {
                    return null;
                }

                string token = GenerateToken(adminAccess);
                AdminAthenticationDTO payload = new AdminAthenticationDTO(principal, adminAccess, token);
                return payload;

            }
            catch (Exception)
            {
                return null;
            }
        }

        private string GenerateToken(AdminAccess adminAccess)
        {
            /*
            * Generate JWT token
            */
            var claims = new
            {
                Guid = adminAccess.AdminFkNav.ADGUID,
                Admin = true,
                SuperAdmin = adminAccess.IsSuperAdmin
            };

            var token = _jwtvService.GenerateJwtToken(JObject.FromObject(claims));
            return token;
        }
    }
}
