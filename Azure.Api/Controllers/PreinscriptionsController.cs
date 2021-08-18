using System.Threading.Tasks;
using Azure.Api.Data.DTOs.Preinscription;
using Azure.Api.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Azure.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowAnyOrigins")]
    public class PreinscriptionsController : ControllerBase
    {
        private readonly ISalesForceService _salesForceService;

        public PreinscriptionsController(ISalesForceService salesForceService)
        {
            _salesForceService = salesForceService;
        }

        /// <summary>
        /// Recupération de tous les status de pré-inscriptions
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetStatutDemande")]
        public async Task<ActionResult> GetStatutDemande()
        {
            var results = await _salesForceService.GetAllStatus();

            return Ok(results);
        }

        [HttpGet("GetPreinscriptions/{id}")]
        public async Task<ActionResult> GetPreinscriptionsBy(string id, bool filtered)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("L'id est vide.");
            }
            var results = await _salesForceService.GetPreinscriptionsBy(id, filtered);

            return Ok(results);
        }

        [HttpPatch("{preinscriptionId}")]
        public async Task<ActionResult> Preinscriptions(string preinscriptionId, PreinscriptionUpdateDto status)
        {
            if (string.IsNullOrEmpty(preinscriptionId) || string.IsNullOrEmpty(status.LPCR_Statut__c))
            {
                return BadRequest("Le preinscriptionID ou le status est vide.");
            }
            var results = await _salesForceService.UpdatePreinscriptionsById(preinscriptionId, status);

            return Ok(results);
        }

        [HttpGet("GetPreinscriptionsIdBy")]
        public async Task<ActionResult> GetPreinscriptionsIdBy(string sfId, string accountId)
        {
            if (string.IsNullOrEmpty(sfId))
            {
                return BadRequest("Le salesforceId est vide.");
            }
            var results = await _salesForceService.FindAllPreinscriptionIdBy(sfId, accountId);

            return Ok(results);
        }
    }
}
