using System.Security.Claims;

namespace Azure.Api.Data.DTOs
{
    public class AdminAthenticationDTO
    {
        public AdminAthenticationDTO(ClaimsPrincipal principal, Models.AdminAccess adminAccess, string token)
        {
            Name = principal.FindFirst("Name").Value;
            Mail = principal.FindFirst("preferred_username").Value;
            IsSuperAdmin = adminAccess.IsSuperAdmin;
            Token = token;
        }

        public string Name { get; set; }
        public string Mail { get; set; }
        public bool IsSuperAdmin { get; set; }
        public string Token { get; set; }
    }
}
