using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Transactions;
using AutoMapper;
using Azure.Api.Data.DTOs;
using Azure.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Azure.Api.Controllers
{
    [Route("api/portal")]
    [ApiController]
    [EnableCors("AllowAnyOrigins")]
    public class PasswordAuthenticationController : ControllerBase
    {
        private readonly IPasswordAuthenticationService _passAuthService;
        private readonly IUserService _userService;
        private readonly IMailService _mailService;
        private readonly IConfiguration _config;
        private readonly IJwtTokenValidatorService _jwtvService;
        private readonly IMapper _mapper;

        public PasswordAuthenticationController(
            IPasswordAuthenticationService passAuthService, 
            IUserService userService, 
            IMailService mailService, 
            IConfiguration config,
            IJwtTokenValidatorService jwtvService,
            IMapper mapper
        )
        {
            _passAuthService = passAuthService ?? throw new ArgumentNullException(nameof(passAuthService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _mailService = mailService ?? throw new ArgumentNullException(nameof(mailService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _jwtvService = jwtvService ?? throw new ArgumentNullException(nameof(jwtvService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody]JObject body)
        {
            try
            {
                var login = body["Login"].ToString();
                var password = body["Password"].ToString();
                var appDomainName = body["AppDomainName"].ToString();

                if (!(await _passAuthService.Authenticate(login, password)))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new HttpErrorMessage("Email et/ou mot de passe incorrect"));
                }

                if (!(await _userService.HasAccess(login, appDomainName)))
                {
                    return StatusCode(420, new HttpErrorMessage("Accès à la platforme restreinte"));
                }

                /*
                 * Temporary code before token approach
                 */
                var user = await _userService.GetUser(login);
                if (user == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new HttpErrorMessage("Echec de récupération des informations"));
                }

                /*
                 * Generate JWT token
                 */
                var claims = new
                {
                    Login = user.Login,
                    Guid = user.GUID,
                    Admin = false
                };

                var token = _jwtvService.GenerateJwtToken(JObject.FromObject(claims));

                /*
                 * Return the user + token
                 */

                var payload = _mapper.Map<AuthenticationDTO>(user);
                payload.Token = token;

                return StatusCode(StatusCodes.Status200OK, payload); 
            }

            catch (Exception)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new HttpErrorMessage("Assurez-vous que les valeurs de point d'entrée dans le corps sont conformes"));
            }
        }

        [HttpPost("pwd/change")] 
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ChangePassword([FromHeader] string authorization, [FromBody]JObject body)
        {
            try
            {
                var login = body["Login"].ToString();
                var oldPassword = body["OldPassword"].ToString();
                var newPassword = body["NewPassword"].ToString();

                if (AuthenticationHeaderValue.TryParse(authorization, out var header))
                {
                    var token = header.Parameter;
                    var validatedToken = _jwtvService.IsTokenValid(token);

                    /*
                     * preventing id usurpation 
                     */
                    var tokenLogin = (_jwtvService.ExtractClaimsFromToken((JwtSecurityToken)validatedToken))["Login"].ToString();
                    if (login != tokenLogin)
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, new HttpErrorMessage($"Votre token ne vous permet pas de changer le mot de passe de { login }"));
                    }
                }

                else
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new HttpErrorMessage("Entête invalide"));
                }

                if (!_passAuthService.MeetsCriterias(newPassword))
                {
                    throw new InvalidCastException();
                }

                var results = await _passAuthService.UpsertPassword(login, newPassword, oldPassword);

                return StatusCode(results.Item1, results.Item2); 
            }

            catch (Exception)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new HttpErrorMessage("Assurez-vous que les valeurs de point d'entrée dans le corps sont conformes"));
            }
        }

        [HttpPost("pwd/reset")]
        public async Task<IActionResult> ResetPassword([FromBody]JObject body)
        {
            string login;

            try
            {
                login = body["Login"].ToString();
            }

            catch (Exception)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new HttpErrorMessage("Assurez-vous que les valeurs de point d'entrée dans le corps sont conformes"));
            }

            var user = await _userService.GetUser(u => u.Login == login && u.IsActive);
            if (user == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new HttpErrorMessage("Assurez vous que le compte existe et est actif"));
            }

            var newPassword = _passAuthService.GeneratePlainPassword();

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var results = await _passAuthService.UpsertPassword(user.GUID, newPassword, verifyOld: false);

                if ((results.Item1 / 100) != 2)
                {
                    return StatusCode(results.Item1, results.Item2);
                }

                if ( !(await SendResetPasswordMail(login, newPassword)) )
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new HttpErrorMessage("Erreur serveur inconnue. Veuillez contacter LPCR pour résoudre l'incident"));
                }

                scope.Complete();

                return StatusCode(results.Item1, results.Item2);
            }
        }

        private async Task<bool> SendResetPasswordMail(string login, string password)
        {
            EmailDTO mail = new EmailDTO
            {
                From = _config.GetValue<string>("FrontFamilleEmail"),
                FromName = "Les Petits Chaperons Rouges",
                To = login,
                Subject = "Votre nouveau mot de passe",
                Body = @$"
                <html>
                <body>    
                    <p>Madame, Monsieur,</p>

                    <p>Vous avez fait une demande de réinitialisation de mot de passe pour votre compte 'Les petits chaperons rouge'.</p>
                    <p>Votre nouveau mot de passe est donc: <b>{ password }</b></p>

                    <br>
                    
                    <p>A très bientôt,</p>
                    <p>Les Petits Chaperons Rouges</p>
                </body>
                </html>
            "
            };

            return await _mailService.SendMail(mail);
        }
    }
}
