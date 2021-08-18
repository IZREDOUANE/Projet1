using Azure.Api.Data.DTOs;
using Azure.Api.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Azure.Api.Services
{
    public interface ISSOAuthenticationService
    {
        /// <summary>
        /// Allows to connect to the app using Azure Active Directory SSO
        /// </summary>
        /// <param name="principal">the azure ad user claims and identity</param>
        /// <returns>a user object to be sent to the user</returns>
        Task<AdminAthenticationDTO> GetUserBySSOLogin(ClaimsPrincipal principal, string appDomainName);
    }
}
