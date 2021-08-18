using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Api.Data.DTOs.Account;
using Azure.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Azure.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountCrecheController : ControllerBase
    {
        private readonly IAccountCrecheService _accountCrecheService;
        private readonly ISalesForceService _salesForceService;
        public readonly IConfiguration _configuration;

        public AccountCrecheController(IAccountCrecheService serviceAccount, IConfiguration configuration, ISalesForceService salesForceService)
        {
            this._accountCrecheService = serviceAccount;
            this._configuration = configuration;
            this._salesForceService = salesForceService;
        }

        [Route("[action]/{parentID}")]
        [EnableCors("AllowAnyOrigins")]
        [HttpGet]
        public async Task<IActionResult> GetAffiliatedCreche(string parentID)
        {
            var creches = await _accountCrecheService.GetCrecheInfoByIdAsync(parentID);
            if (creches == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Failed to retrieve affiliated creche");
            }

            else
            {
                return Ok(creches);
            }
        }

        [Route("creche/get/{crecheID}")]
        [EnableCors("AllowAnyOrigins")]
        [HttpGet]
        public async Task<IActionResult> GetCrecheObjectById(string crecheID)
        {
            var creches = await _accountCrecheService.GetCrecheInfoByCrecheSfIdAsync(crecheID);
            if (creches == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Failed to retrieve affiliated creche");
            }

            else
            {
                return Ok(creches);
            }
        }

        [HttpPost("creche/employee/get")]
        [EnableCors("SSOAccessPolicy")]
        [Authorize]
        public async Task<IActionResult> GetCrechesViewForEmployee([FromBody] dynamic employee)
        {
            var error500 = StatusCode(StatusCodes.Status500InternalServerError, $"Failed to retrieve creches");
            var error401 = StatusCode(StatusCodes.Status401Unauthorized, $"You are nowhere to be seen in Salesforce base");

            Dictionary<string, string> empIdentity = JsonConvert.DeserializeObject<Dictionary<string, string>>(employee.ToString());


            if (!empIdentity.ContainsKey("email"))
            {
                return BadRequest("crecheId property is missing");
            }

            string email = empIdentity["email"];

            //get the sfId through Salesforce
            string sfId = await _salesForceService.GetCollaborateurSfIdFromEmailAsync(email);
            if (String.IsNullOrEmpty(sfId))
            {
                return error401;
            }

            if (String.IsNullOrEmpty(sfId) || String.IsNullOrEmpty(email))
            {
                return BadRequest("make sure sfId and email are not empty strings");
            }

            if (email.Length > 1 && Char.IsLetter(email[0]) && email[1] == '.')
            {
                var creches = await _accountCrecheService.GetallCrechesNameAndIdAsync();
                if (creches == null)
                {
                    return error500;
                }

                else
                {
                    return Ok(creches);
                }
            }

            else
            {
                var creche = await _accountCrecheService.GetDirectorCrecheNameAndIdAsync(sfId);
                if (creche == null)
                {
                    return error500;
                }

                else
                {
                    return Ok(creche);
                }
            }
        }

        [HttpPost("creche/list/families")]
        [EnableCors("SSOAccessPolicy")]
        [Authorize]
        public async Task<IActionResult> GetListOfFamiliesFromCreche([FromBody] dynamic creche)
        {
            try
            {
                var crecheInfos = JsonConvert.DeserializeObject<Dictionary<string, string>>(creche.ToString());

                if (!crecheInfos.ContainsKey("crecheId"))
                {
                    return BadRequest("crecheId property is missing");
                }

                var families = await this._accountCrecheService.GetListOfFamiliesFromCreche(crecheInfos["crecheId"]);
                if (families == null)
                {
                    throw new Exception();
                }

                return Ok(families);
            }

            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }

        [HttpGet("creche/employee/get/list")]
        [EnableCors("SSOAccessPolicy")]
        [Authorize]
        public async Task<IActionResult> GetListTest()
        {
            var error500 = StatusCode(StatusCodes.Status500InternalServerError, $"Failed to retrieve creches");
            var creches = await _accountCrecheService.GetallCrechesNameAndIdAsync();
            if (creches == null)
            {
                return error500;
            }

            else
            {
                return Ok(creches);
            }
        }
    }
}
