using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Azure.Api.Data.DTOs;
using Azure.Api.Data.Models;
using Azure.Api.Services;
using HeyRed.Mime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Azure.Api.Controllers
{
    [Route("api/ged")]
    [ApiController]
    [EnableCors("AllowAnyOrigins")]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly IJwtTokenValidatorService _jwtvService;

        public DocumentController(IDocumentService docservice, IJwtTokenValidatorService jwtvService)
        {
            _documentService = docservice;
            _jwtvService = jwtvService;
        }

        [HttpPost("upload")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Create([FromBody]JObject body)
        {
            try
            {
                var docQuery = body.ToObject<DocumentUploadDTO>();
                if (!docQuery.IsPublic && (docQuery.AuthorizedUsersSfId == null || docQuery.AuthorizedUsersSfId.Length == 0))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new HttpErrorMessage("Vous devez soit déclarer que le document est public ou que le document dispose d'au moins un utilisateur autorisé"));
                }

                var content = Convert.FromBase64String(docQuery.Content);

                var results = await _documentService.Create(docQuery.Details, content, docQuery.AuthorizedUsersSfId, docQuery.IsPublic);
                return StatusCode(results.Item1, results.Item2);
            }

            catch (Exception)
            {
                return StatusCode(400, new HttpErrorMessage("Assurez-vous que les valeurs de point d'entrée dans le corps sont conformes"));
            }
        }

        [HttpPost("upload/bulk")]
        // [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> CreateBulk([FromBody]JObject body)
        {
            try
            {
                var docQueries = body["Documents"].ToObject<DocumentUploadDTO[]>();
                var failedDocuments = new List<string>();

                foreach (var docQuery in docQueries)
                {
                    if (!docQuery.IsPublic && (docQuery.AuthorizedUsersSfId == null || docQuery.AuthorizedUsersSfId.Length == 0))
                    {
                        var docId = $"{ docQuery.Details?.FileName }/{ docQuery.Details?.FileCategory }/{ docQuery.Details?.ModifiedBy }";
                        failedDocuments.Add($"{ docId } - Erreur 400: Vous devez soit déclarer que le document est public ou que le document dispose d'au moins un utilisateur autorisé");
                    }

                    else
                    {
                        var content = Convert.FromBase64String(docQuery.Content);

                        var results = await _documentService.Create(docQuery.Details, content, docQuery.AuthorizedUsersSfId, docQuery.IsPublic);
                        if ((results.Item1 / 100) != 2)
                        {
                            var docId = $"{ docQuery.Details?.FileName }/{ docQuery.Details?.FileCategory }/{ docQuery.Details?.ModifiedBy }";
                            failedDocuments.Add($"{ docId } - Erreur { results.Item1 }: { ((HttpErrorMessage)results.Item2).ErrorMessage }");
                        }
                    }
                }

                return StatusCode(StatusCodes.Status200OK, new
                {
                    TotalUploaded = docQueries.Length - failedDocuments.Count,
                    ErrorMessages = failedDocuments
                });
            }

            catch (Exception)
            {
                return StatusCode(400, new HttpErrorMessage("Assurez-vous que les valeurs de point d'entrée dans le corps sont conformes"));
            }
        }

        [HttpPost("update")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Update([FromHeader]string authorization, [FromBody]JObject body)
        {
            try
            {
                bool isAdmin = false;
                if (AuthenticationHeaderValue.TryParse(authorization, out var header))
                {
                    var token = header.Parameter;
                    var validatedToken = _jwtvService.IsTokenValid(token);

                    isAdmin = (_jwtvService.ExtractClaimsFromToken((JwtSecurityToken)validatedToken))["Admin"].ToObject<bool>();
                }

                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new HttpErrorMessage("Token invalide"));
                }

                var details = body["Details"].ToObject<DocumentDTO>();
                var ownership = (body.ContainsKey("Ownership"))? body["Ownership"].ToString() : string.Empty;
                var content = Convert.FromBase64String(body["Content"].ToString());

                var results = await _documentService.Update(details, content, ownership, isAdmin);

                return StatusCode(results.Item1, results.Item2);
            }

            catch (Exception)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new HttpErrorMessage("Assurez-vous que les valeurs de point d'entrée dans le corps sont conformes"));
            }
        }

        [HttpGet("download")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Download(Guid blobGUID, string userSfId="")
        {
            using (var downloadPayload = await _documentService.Download(userSfId, blobGUID))
            {
                if ((downloadPayload.HttpResponseCode / 100) != 2)
                {
                    return StatusCode(downloadPayload.HttpResponseCode, new HttpErrorMessage(downloadPayload.HttpResponseMessage));
                }

                return File(downloadPayload.StreamContent.ToArray(), MimeTypesMap.GetMimeType(downloadPayload.FileExtension), downloadPayload.FileName);
            }
        }

        [HttpPatch("delete")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Delete([FromHeader]string authorization, string ownership="", Guid? docGuid=null, string docSfId=null)
        {
            if (AuthenticationHeaderValue.TryParse(authorization, out var header))
            {
                var token = header.Parameter;
                var validatedToken = _jwtvService.IsTokenValid(token);

                var isAdmin = (_jwtvService.ExtractClaimsFromToken((JwtSecurityToken)validatedToken))["Admin"].ToObject<bool>();

                var results = await _documentService.Delete(ownership, docGuid, docSfId, isAdmin);
                return StatusCode(results.Item1, results.Item2);
            }

            else
            {
                return StatusCode(StatusCodes.Status400BadRequest, new HttpErrorMessage("Entête invalide"));
            }
        }

        [HttpGet("list")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ListDocumentsById(string id, string docTypes = null, DocumentState docState = DocumentState.CURRENT)
        {
            HashSet<string> docTypesHashSet = null;
            if (!string.IsNullOrEmpty(docTypes))
            {
                docTypesHashSet = new HashSet<string>(docTypes.Split(';'));
            }

            var docList = await _documentService.ListById(id, docState, docTypesHashSet);
            if (docList == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new HttpErrorMessage($"La récupération du document { id } a échoué"));
            }

            return StatusCode(StatusCodes.Status200OK, docList);
        }

        [HttpGet("types")]
        public async Task<IActionResult> ListAllDocumentTypes()
        {
            try
            {
                var docTypes = await _documentService.ListAllTypes();
                if (docTypes == null)
                {
                    throw new InvalidOperationException();
                }

                return StatusCode(StatusCodes.Status200OK, docTypes);
            }

            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new HttpErrorMessage($"Erreur serveur interne: {ex}"));
            }
        }

        [HttpPut("reprise_documents")]
        public async Task<IActionResult> AddRangeRepriseDocuments([FromBody] JObject data)
        {
            if (!data.ContainsKey("documents"))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new HttpErrorMessage("Ausun document n'a été trouvé dans le corps"));
            }

            try
            {
                List<RepriseDocument> documents = data["documents"].ToObject<List<RepriseDocument>>();
                var result = await this._documentService.AddRepriseDocumentsToDedicatedTable(documents);
                if (!result)
                {
                    throw new InvalidOperationException("Echec d'ajout d'objets dans la table désirée");
                }

                return StatusCode(StatusCodes.Status200OK, new { resp = true });
            }

            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new HttpErrorMessage("Echec de mis à jour des information"));
            }
        }
    }
}