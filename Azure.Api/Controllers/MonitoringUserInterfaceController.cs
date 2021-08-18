using Azure.Api.Services.Monitoring;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;

namespace Azure.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowAnyOrigins")]
    public class MonitoringUserInterfaceController : ControllerBase
    {
        private readonly IMonitoringService _monitoringService;

        public MonitoringUserInterfaceController(IMonitoringService monitoringService)
        {
            _monitoringService = monitoringService;
        }

        [HttpGet("FindAll")]
        public IActionResult FindAll()
        {
            var results = _monitoringService.FindAll();

            return Ok(results);
        }

        [HttpGet("GetFileLog/{monitoringId}")]
        public IActionResult GetFileLog(int monitoringId)
        {
            try
            {
                var monitoring = _monitoringService.GetByIdAsync(monitoringId).Result;
                if ((bool)!monitoring?.IsSuccess)
                {
                    var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    var filename = $"fileLog-{date}.log";

                    return CreateFile(filename, monitoring.Erreur);
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return Ok($"Aucun fichier log trouvé pour monitoringFlux avec ID: {monitoringId}");
        }

        private IActionResult CreateFile(string filename, string datas)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return Ok("Le nom du fichier est vide.");
            }
            if (string.IsNullOrEmpty(datas))
            {
                return Ok("Les données sont vides.");
            }

            return File(Encoding.ASCII.GetBytes(datas), "text/plain", filename);
        }
    }
}
