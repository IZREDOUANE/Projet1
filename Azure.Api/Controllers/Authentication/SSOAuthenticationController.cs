using System;
using System.Threading.Tasks;
using Azure.Api.Data.DTOs;
using Azure.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Azure.Api.Controllers
{

    [Route("api/portal/sso")]
    [ApiController]
    [Authorize(AuthenticationSchemes = OpenIdConnectDefaults.AuthenticationScheme)]
    [EnableCors("AllowAnyOrigins")]
    public class SSOAuthenticationController : ControllerBase
    {
        private readonly ISSOAuthenticationService _ssoService;

        public SSOAuthenticationController(ISSOAuthenticationService ssoService)
        {
            this._ssoService = ssoService;
        }

        [Route("login")]
        [HttpGet]
        public async Task<IActionResult> Login(string appDomainName)
        {
            if (appDomainName == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest, "Please ensure that you respected the guidelines for input of the entrypoint");
            }
            try
            {
                var result = await HttpContext.AuthenticateAsync(OpenIdConnectDefaults.AuthenticationScheme);

                if (result?.Principal != null)
                {
                    AdminAthenticationDTO azureAdUser = await _ssoService.GetUserBySSOLogin(result.Principal, appDomainName);

                    if (azureAdUser == null)
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, $"Access denied.");
                    }
                    else
                    {
                        return Ok(azureAdUser);
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }
    }
}