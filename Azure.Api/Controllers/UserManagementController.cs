using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
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
    [Route("api/portal/user")]
    [ApiController]
    [EnableCors("AllowAnyOrigins")]
    public class UserManagementController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IPasswordAuthenticationService _pwdAuthService;
        private readonly IMailService _mailService;
        private readonly IConfiguration _config;
        private readonly IJwtTokenValidatorService _jwtvService;
        private readonly IMapper _mapper;

        public UserManagementController(IUserService userService, IPasswordAuthenticationService pwdAuthService, IMailService mailService, IJwtTokenValidatorService jwtvService, IMapper mapper, IConfiguration config)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _pwdAuthService = pwdAuthService ?? throw new ArgumentNullException(nameof(pwdAuthService));
            _mailService = mailService ?? throw new ArgumentNullException(nameof(mailService));
            _jwtvService = jwtvService ?? throw new ArgumentNullException(nameof(jwtvService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <returns>[TEMP] a user </returns>
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody]JObject infos)
        {
            try
            {
                var user = infos["Infos"].ToObject<UserDTO>();
                var userSfLinks = infos["SalesforceAccounts"].ToObject<string[]>();
                var authorizedPlaforms = infos["AuthorizedPlatforms"].ToObject<string[]>();

                if (string.IsNullOrEmpty(user.Login))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new HttpErrorMessage("Veuillez insérer une adresse mail correct"));
                }

                if (authorizedPlaforms.Length == 0)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new HttpErrorMessage("Veuillez insérer autorisé le nouveau compte à au moins une plateforme LPCR valide"));
                }

                using (var scope = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
                {
                    if ((await _userService.GetUser(u => u.Login == user.Login) != null))
                    {
                        return StatusCode(StatusCodes.Status400BadRequest, new HttpErrorMessage($"Adresse électronique déjà utilisée"));
                    }

                    var insertedUser = await _userService.UpsertUser<UserDTO>(user, userSfLinks, authorizedPlaforms);
                    if (insertedUser == null)
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, new HttpErrorMessage("Echec de création du compte utilisateur"));
                    }

                    else
                    {
                        var newPassword = _pwdAuthService.GeneratePlainPassword();
                        var pwdInsertResults = await _pwdAuthService.UpsertPassword(insertedUser.GUID, newPassword);

                        if ((pwdInsertResults.Item1 / 100) != 2)
                        {
                            return StatusCode(StatusCodes.Status500InternalServerError, new HttpErrorMessage("Echec de création du compte utilisateur"));
                        }

                        if (!(await SendActivateUserMail(insertedUser.Login, insertedUser.ConfirmEmailToken, newPassword)))
                        {
                            return StatusCode(StatusCodes.Status500InternalServerError, new HttpErrorMessage("Echec de création du compte utilisateur"));
                        }

                    }

                    scope.Complete();
                }

                return StatusCode(StatusCodes.Status201Created, new HttpSuccessMessage("Un courrier électronique vous sera envoyé au destinaire pour la finalisation de création de compte"));
            }

            catch
            {
                return StatusCode(StatusCodes.Status400BadRequest, new HttpErrorMessage("Assurez-vous que les valeurs de point d'entrée dans le corps sont conformes"));
            }
        }

        private async Task<bool> SendActivateUserMail(string login, Guid? emailToken, string newPassword)
        {
            if (string.IsNullOrEmpty(login))
            {
                return false;
            }

            try
            {
                string url = $"{ Request.Scheme }://{ Request.Host.Value }/api/portal/user/activate";

                EmailDTO mailPayload = new EmailDTO();
                mailPayload.From = _config.GetValue<string>("FrontFamilleEmail");
                mailPayload.To = login;

                mailPayload.Subject = "Bienvenue dans votre nouveau portail LPCR";

                mailPayload.FromName = "Les Petits Chaperons Rouges";

                mailPayload.Body = @$"
                <html>
                <body>
                    <p>Bonjour,</p>
                    <p>Vous avez créé un compte dans notre plateforme LPCR et nous vous en remercions.</p>
                    <p>Vos identifiants pour accéder à notre portail sont les suivants:</p>

                    <span>
                        <ul>
                            <li><b>Identifiant:</b> { login }</li>
                            <li><b>Mot de passe:</b> { newPassword }</li>
                        </ul>
                    </span>

                    <p>Votre mot peut être changé à tout moment sur votre portail</p>
                
                    <p>Afin de finaliser votre compte, veuillez cliquer sur le lien suivant: <a href=""{ url }?emailToken={ emailToken }""><b>activer le compte</b></a> </p>

                    <p>Si vous avez des questions concernant votre compte portail LPCR, vous pouvez nous contacter via le formulaire disponible sur le site <a href=""https://www.lpcr.fr/fr/contact"">lpcr.fr</a></p>

                    <p>Cordialement,</p>
                    <p>L'équipe Commerciale LPCR</p>
                </body>
                </html>
            ";

                return await _mailService.SendMail(mailPayload);
            }

            catch (Exception)
            {
                return false;
            }
        }

        [HttpGet("activate")]
        public async Task<IActionResult> ActivateUser(Guid? emailToken)
        {
            if (emailToken == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new HttpErrorMessage("Token email invalide"));
            }

            var results = await _userService.EnableUser(u => u.ConfirmEmailToken == emailToken);

            return StatusCode(results.Item1, results.Item2);
        }

        [HttpGet("coordinates/details")]
        [Authorize(AuthenticationSchemes =JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetUserCoordinatesFullDetails([FromHeader] string authorization, Guid? userGUID)
        {
            var claims = GetTokenClaims(authorization);

            try
            {
                var isAdmin = claims["Admin"].ToObject<bool>();
                if (isAdmin)
                {
                    if ((userGUID == null || userGUID == Guid.Empty))
                    {
                        return StatusCode(StatusCodes.Status400BadRequest, new HttpErrorMessage("Veuillez indiquer un Guid utilisateur valide"));
                    }
                }

                else
                {
                    var tokenUserGuid = claims["Guid"].ToObject<Guid>();
                    if (tokenUserGuid == null || tokenUserGuid == Guid.Empty || (userGUID != null && userGUID != Guid.Empty && tokenUserGuid != userGUID))
                    {
                        return StatusCode(StatusCodes.Status401Unauthorized, new HttpErrorMessage("Vous n'êtes pas autorisé à accéder à cette ressource"));
                    }

                    userGUID = (userGUID == null || userGUID == Guid.Empty)? tokenUserGuid : userGUID;
                }
            }

            catch (Exception)
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new HttpErrorMessage("Votre token est invalide"));
            }    

            var results = await _userService.GetUser(userGUID);
            if (results == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new HttpErrorMessage("L'utilisateur est inconnu"));
            }

            var payload = _mapper.Map<UserDTO>(results);
            return StatusCode(StatusCodes.Status200OK, payload);
        }

        [HttpPut("update/coordinates")]
        public async Task<IActionResult> UpdateUserCoordinates([FromHeader] string authorization, [FromBody] JObject body)
        {
            string login;
            UserDTO newCoords;

            try
            {
                if (body.ContainsKey("Login"))
                {
                    login = body["Login"].ToString();
                }

                else
                {
                    var claims = GetTokenClaims(authorization);
                    login = claims["Login"].ToString();

                    if (string.IsNullOrEmpty(login))
                    {
                        throw new ArgumentNullException(nameof(login));
                    }
                }
                
                newCoords = body["Coordinates"].ToObject<UserDTO>();
            }

            catch (Exception)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new HttpErrorMessage("Le corps de la requête est incorrect"));
            }

            var isAuthorized = CheckCallerAuthorityThroughToken(authorization, login);
            if ((isAuthorized.Item1 / 100) != 2)
            {
                return StatusCode(isAuthorized.Item1, isAuthorized.Item2);
            }

            bool isSuccess = await _userService.UpdateUserCoordinates(login, newCoords);
            if (!isSuccess)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new HttpErrorMessage("Echec de la mise des coordonnées utilisateur"));
            }

            return StatusCode(StatusCodes.Status200OK, new HttpSuccessMessage("L'utilisateur a été mis à jour"));
        }

        [HttpPost("sf_accounts")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetSfAccountsFromUser([FromHeader]string authorization, [FromBody]JObject body)
        {
            if (!body.ContainsKey("login"))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new HttpErrorMessage("Propriété 'login' introuvable dans le corps de la requête"));
            }

            var login = body["login"].ToString();

            var callerAuthorized = CheckCallerAuthorityThroughToken(authorization, login);
            if ((callerAuthorized.Item1 / 100) != 2)
            {
                return StatusCode(callerAuthorized.Item1, callerAuthorized.Item2);
            }

            var sfAccounts = await _userService.GetUserSfAccounts(login);
            if (sfAccounts == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new HttpErrorMessage("Echec de récupération des comptes sf associés. Assurez que le login existe"));
            }

            else
            {
                return StatusCode(StatusCodes.Status200OK, sfAccounts.Select(a => a.SalesforceAccountId));
            }
        }

        private Tuple<int, IHttpMessage> CheckCallerAuthorityThroughToken(string authorization, string login)
        {
            try
            {
                if (string.IsNullOrEmpty(login))
                {
                    throw new ArgumentException("Echec de récupération du login utilisateur");
                }

                if (string.IsNullOrEmpty(authorization))
                {
                    return Tuple.Create(StatusCodes.Status400BadRequest, new HttpErrorMessage("Le Token JWT est manquant") as IHttpMessage);
                }

                var claims = GetTokenClaims(authorization);
                if (claims == null)
                {
                    return Tuple.Create(StatusCodes.Status401Unauthorized, new HttpErrorMessage("Votre token ne contient aucune propriétés vous permettant votre autorisation à la ressource demandée") as IHttpMessage);
                }

                var isAdmin = claims["Admin"].ToObject<bool>();
                var tokenLogin = claims.ContainsKey("Login") ? claims["Login"].ToString() : string.Empty;

                if (!isAdmin && login.CompareTo(tokenLogin) != 0)
                {
                    return Tuple.Create(StatusCodes.Status401Unauthorized, new HttpErrorMessage("Vous n'avez pas les droits pour consulter aux ressourcs associés à cet utilisateur") as IHttpMessage);
                }

                return Tuple.Create(StatusCodes.Status200OK, new HttpSuccessMessage("Utilisateur autorisé à utiliser ce login") as IHttpMessage);
            }

            catch (Exception)
            {
                return Tuple.Create(StatusCodes.Status400BadRequest, new HttpErrorMessage("Assurez-vous que les valeurs de point d'entrée dans le corps sont conformes") as IHttpMessage);
            }

        }

        private JObject GetTokenClaims(string authorization)
        {
            if (AuthenticationHeaderValue.TryParse(authorization, out var header))
            {
                var token = header.Parameter;
                var validatedToken = _jwtvService.IsTokenValid(token);

                return _jwtvService.ExtractClaimsFromToken((JwtSecurityToken)validatedToken);
            }

            return null;
        }
    }
}
