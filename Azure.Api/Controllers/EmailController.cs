using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Api.Data.DTOs;
using Azure.Api.Data.DTOs.Email;
using Azure.Api.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Azure.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowAnyOrigins")]
    public class EmailController : ControllerBase
    {

        private readonly IMailService _emailService;

        public EmailController(IMailService mailService)
        {
            this._emailService = mailService;
        }

        [HttpPost]
        [Route("send")]
        public async Task<IActionResult> Send([FromBody] JObject body)
        {
            try
            {
                // TBD for retrieving documents not from Salesforce (no way to retrieve GUID of the document)

                var infos = body["Infos"].ToObject<EmailDTO>();

                EmailDocumentDTO[] documents;
                if (body.ContainsKey("Documents"))
                {
                    documents = body["Documents"].ToObject<EmailDocumentDTO[]>();
                }

                else
                {
                    documents = new EmailDocumentDTO[0];
                }

                if ( !(await _emailService.SendMail(infos, documents)))
                {
                    StatusCode(StatusCodes.Status500InternalServerError, "Failed to send email to recipient");
                }
            }

            catch (Exception)
            {
                return StatusCode(StatusCodes.Status400BadRequest, "Please ensure the body respects the guidelines of the entrypoint");
            }

            return StatusCode(StatusCodes.Status200OK, "Email has been sent successfully");
        }
    }
}