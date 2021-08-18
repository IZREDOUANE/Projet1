using System;
using System.Threading.Tasks;
using Azure.Api.Data.DTOs;
using Azure.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Azure.Api.Controllers
{
    [Route("api/intranet/view")]
    [ApiController]
    public class IntranetViewerController : ControllerBase
    {
        private readonly IIntranetViewerService _ivService;

        public IntranetViewerController(IIntranetViewerService ivService)
        {
            _ivService = ivService;
        }

        [HttpGet("clients")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ViewClients(string search="", int page=1, int pageSize=0)
        {
            var results = await _ivService.ListLpcrClients(search, Math.Max(page-1, 0), pageSize);

            if ((results.Item1 / 100) == 2)
            {
                return Content((results.Item2 as HttpSuccessMessage).SuccessMessage, new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("application/json"));
            }

            else
            {
                return StatusCode(results.Item1, results.Item2);
            }
        }

        [HttpGet("reprise_documents/list")]
        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ViewRepriseDocuments(int page=1, int pageSize=0)
        {
            var results = await _ivService.ListLpcrRepriseDocuments(Math.Max(page - 1, 0), pageSize);

            if ((results.Item1 / 100) == 2)
            {
                return Content((results.Item2 as HttpSuccessMessage).SuccessMessage, new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("application/json"));
            }

            else
            {
                return StatusCode(results.Item1, results.Item2);
            }
        }

        [HttpPost("reprise_documents/download")]
        public async Task<IActionResult> DownloadRepriseDocument([FromBody] JObject body)
        {
            if (!body.ContainsKey("Path"))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new HttpErrorMessage("Il manque l'attribut path dans votre corps de requête HTTP"));
            }

            var path = body["Path"].ToString();

            var results = await _ivService.DownloadLpcrRepriseDocument(path);

            if ((results.Item1 / 100) == 2)
            {
                return Content((results.Item2 as HttpSuccessMessage).SuccessMessage, new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("application/json"));
            }

            else
            {
                return StatusCode(results.Item1, results.Item2);
            }
        }

        [HttpGet("test")]
        public async Task<IActionResult> TestCommunicationWithLpcrAPI()
        {
            var results = await _ivService.TestCommunicationWithLpcrApi();
            return StatusCode(results.Item1, results.Item2);
        }
    }
}
