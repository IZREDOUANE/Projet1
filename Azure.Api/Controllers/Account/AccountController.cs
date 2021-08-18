using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Azure.Api.Services;
using Azure.Api.Data.DTOs;
using Microsoft.Extensions.Configuration;
using Azure.Api.Data.DTOs.Account;
using Azure.Api.Services.Entreprise;
using Microsoft.AspNetCore.Cors;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;

namespace Azure.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {

        private readonly IAccountFamilleService _acccountFamilleService;
        private readonly IAccountEntrepriseService _acccountEntrepriseService;
        private readonly IUserService _userService;
        private readonly IMailService _emailService;
        private readonly IPasswordAuthenticationService _pwdAuthService;
        public readonly IConfiguration configuration;
   

        public AccountController(
            IAccountFamilleService serviceAcccount, 
            IAccountEntrepriseService serviceEntrepriseAccount, 
            IMailService emailService,
            IConfiguration configuration, 
            IUserService userService, 
            IPasswordAuthenticationService pwdAuthService
        )
        {
            this._acccountFamilleService = serviceAcccount;
            this._acccountEntrepriseService = serviceEntrepriseAccount;
            this._emailService = emailService;
            this.configuration = configuration;

            _userService = userService;
            _pwdAuthService = pwdAuthService;
        }

        [HttpGet("Get/Contacts")]
        [EnableCors("AllowAnyOrigins")]
        public async Task<List<ContactParentEnfantDTO>> GetAccountContacs(string sfId)
        {
            return await _acccountFamilleService.GetContactsBySFId(sfId);
        }

        [HttpGet("Get/Famille")]
        [EnableCors("SSOAccessPolicy")]
        [Authorize]
        public async Task<IActionResult> GetAccountFamilleBySfId(string sfId)
        {
            var accountFamille = await _acccountFamilleService.GetAccountFamilleBySfId(sfId);
            if (accountFamille == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Account could not be found in SF, please check if sfId is valid");
            }

            return Ok(accountFamille);
        }

        [HttpPost("Get/Famille/details")]
        [EnableCors("AllowAnyOrigins")]
        public async Task<IActionResult> GetAccountFamilleInformationDetailsBySfId([FromBody] dynamic criteria)
        {
            var param = JsonConvert.DeserializeObject<Dictionary<string, string>>(criteria.ToString());
            if (!param.ContainsKey("sfId"))
            {
                return BadRequest("No sfId parameter was found");
            }

            string sfId = param["sfId"];

            var accountFamilleDetails = await _acccountFamilleService.GetAccountFamilleInformationDetailsBySfId(sfId);
            if (accountFamilleDetails == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Account could not be found in SF, please check if sfId is valid");
            }

            return Ok(accountFamilleDetails);
        }

        [HttpPut("Update/Famille/details")]
        [EnableCors("AllowAnyOrigins")]
        public async Task<IActionResult> UpdateAccountFamilleInformationDetailsBySfId([FromBody]JObject data)
        {
            if (!data.ContainsKey("sfId"))
            {
                return BadRequest("No SfId parameter was found in your request");
            }

            if (!data.ContainsKey("accountData"))
            {
                return BadRequest("No accountData parameter was found in your request");
            }

            try
            {
                string sfId = data["sfId"].ToString();
                AccountFamilleDTO account = data["accountData"].ToObject<AccountFamilleDTO>();

                bool success = await _acccountFamilleService.UpdateAccountFamilyInformationDetails(sfId, account);
                if (!success)
                {
                    throw new Exception("Internal error with Salesforce error, please check if attributes and value are correctly written");
                }

                return Ok(true);
            }

            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update for your information details");
            }
        }

        [HttpPut("update/client/details")]
        [EnableCors("AllowAnyOrigins")]
        public async Task<IActionResult> UpdateAccountEntrepriseDetailsBySfId([FromBody]JObject data)
        {
            if (!data.ContainsKey("sfId"))
            {
                return BadRequest("No SfId parameter was found in your request");
            }

            if (!data.ContainsKey("accountData"))
            {
                return BadRequest("No accountData parameter was found in your request");
            }

            string sfId = data["sfId"].ToString();
            AccountEntrepriseDTO account = data["accountData"].ToObject<AccountEntrepriseDTO>();

            bool success = await _acccountEntrepriseService.UpdateAccountEntreprise(sfId, account);
            if (!success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update for your information details");
            }

            return Ok(true);
        }

        [HttpPost("Get/Familles")]
        [EnableCors("SSOAccessPolicy")]
        [Authorize]
        public async Task<IActionResult> GetAccountsFamillesBySfId(dynamic criteria)
        {
            try
            {
                Dictionary<string, List<string>> sfIds = JsonConvert.DeserializeObject < Dictionary<string, List<string>> > (criteria.ToString());
                if (!sfIds.ContainsKey("sfIds"))
                {
                    return BadRequest();
                }

                var accounts = new Dictionary<string, AccountFamilleDTO>();
                foreach (var sfId in sfIds["sfIds"])
                {
                    var account = await _acccountFamilleService.GetAccountFamilleBySfId(sfId);

                    if (account != null)
                    {
                        accounts.Add(sfId, account);
                    } 
                }

                return Ok(accounts);
            }

            catch(Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }

        [HttpPost("Add/Contact")]
        [EnableCors("AllowAnyOrigins")]
        public async Task<ContactParentEnfantDTO> CreateContact(string sfID, [FromBody] ContactParentEnfantDTO contact)
        {
            return await _acccountFamilleService.CreateAccountContactPE(sfID, contact);
        }

        [HttpPost("Delete/Contact")]
        [EnableCors("AllowAnyOrigins")]
        public async Task<bool> DeleteContact(string sfID, [FromBody] ContactParentEnfantDTO contact)
        {
            return await _acccountFamilleService.DeleteContactPE(sfID, contact);
        }

        /// <summary>
        /// Checks whether an employe is a director or an admin (all other types)
        /// </summary>
        /// <param name="employee">employee body request</param>
        /// <returns>account type code</returns>
        [HttpPost("employee/check/type")]
        [EnableCors("SSOAccessPolicy")]
        [Authorize]
        public IActionResult CheckEmployeeType([FromBody] dynamic employee)
        {
            var error500 = StatusCode(StatusCodes.Status500InternalServerError, $"Failed to retrieve creches");

            Dictionary<string, string> empIdentity = JsonConvert.DeserializeObject<Dictionary<string, string>>(employee.ToString());

            if (!empIdentity.ContainsKey("email"))
            {
                return error500;
            }

            string email = empIdentity["email"];
            if (String.IsNullOrEmpty(email))
            {
                return error500;
            }

            if (email.Length > 1 && Char.IsLetter(email[0]) && email[1] == '.')
            {
                return Ok(0x000000); // admin
            }

            else
            {
                return Ok(0x000001); // directrice
            }
        }

        [HttpPost("get/client-infos")]
        [EnableCors("AllowAnyOrigins")]
        public async Task<IActionResult> GetAccountEntInfos([FromBody] dynamic body)
        {
            string sfId = string.Empty;

            try
            {
                var props = JsonConvert.DeserializeObject<Dictionary<string, string>>(body.ToString());
                if (!props.ContainsKey("sfId"))
                {
                    throw new Exception();
                }

                sfId = props["sfId"];
            }

            catch (Exception)
            {
                return StatusCode(StatusCodes.Status400BadRequest, "Please put the 'sfId' of the account in the body of your HTTP request");
            }

            try
            {
                var payload = await _acccountEntrepriseService.GetAccountInfos(sfId);
                if (payload == null)
                {
                    throw new Exception();
                }

                return Ok(payload);
            }

            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve account infos");
            }
        }

        [HttpPost("get/client-contacts-lpcr")]
        [EnableCors("AllowAnyOrigins")]
        public async Task<IActionResult> GetContactsLpcr([FromBody] dynamic body)
        {
            string sfId = string.Empty;

            try
            {
                var props = JsonConvert.DeserializeObject<Dictionary<string, string>>(body.ToString());
                if (!props.ContainsKey("sfId"))
                {
                    throw new Exception();
                }

                sfId = props["sfId"];
            }

            catch (Exception)
            {
                return StatusCode(StatusCodes.Status400BadRequest, "Please put the 'sfId' of the account in the body of your HTTP request");
            }

            try
            {
                var payload = await _acccountEntrepriseService.GetContactsLpcr(sfId);
                if (payload == null)
                {
                    throw new Exception();
                }

                return Ok(payload);
            }

            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve the contacts of lpcr employees in charge of the client's subscription");
            }
        }
    }
}