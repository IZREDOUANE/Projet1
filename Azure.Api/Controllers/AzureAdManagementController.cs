using Azure.Api.Data.DTOs;
using Azure.Api.Data.DTOs.AzureAd;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Azure.Api.Controllers
{
    [Route("api/portal/azureAd")]
    [ApiController]
    [Authorize(AuthenticationSchemes = OpenIdConnectDefaults.AuthenticationScheme)]
    [EnableCors("AllowAnyOrigins")]
    public class AzureAdManagementController : ControllerBase
    {
        private readonly GraphServiceClient _graphServiceClient;

        public AzureAdManagementController(GraphServiceClient graphServiceClient)
        {
            _graphServiceClient = graphServiceClient;
        }

        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        [Route("users")]
        [HttpGet]
        public async Task<IActionResult> GetAzureAdUsers(string displayName = "", string mail = "", int page = 0, int pageSize = 100)
        {
            if (displayName is null)
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            try
            {
                List<User> users = new List<User>();
                var usersPage = await _graphServiceClient.Users
                    .Request()
                    .GetAsync();

                // Add the first page of results to the user list
                users.AddRange(usersPage.CurrentPage);

                // Fetch each page and add those results to the list
                while (usersPage.NextPageRequest != null)
                {
                    usersPage = await usersPage.NextPageRequest.GetAsync();
                    users.AddRange(usersPage.CurrentPage);
                }

                IEnumerable<AzureAdUserDTO> azureAdUserList = users
                    .Where(x => x.DisplayName.Contains(displayName) && x.UserPrincipalName.Contains(mail))
                    .Select(u => new AzureAdUserDTO
                    {
                        DisplayName = u.DisplayName,
                        Mail = u.UserPrincipalName,
                        BusinessPhones = u.MobilePhone,
                        Id = u.Id
                    })
                    .Skip(pageSize * page)
                    .Take(pageSize);

                return Ok(new AzureAdListDTO(azureAdUserList.Count(), azureAdUserList));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }
    }
}
