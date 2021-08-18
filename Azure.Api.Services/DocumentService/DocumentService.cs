using Azure.Api.Data.DTOs;
using Azure.Api.Data.DTOs.Document;
using Azure.Api.Data.Models;
using Azure.Api.Repository;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using System.IO;
using AutoMapper;
using System.Transactions;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Azure.Api.Services
{
    public enum DocumentState
    {
        ANY = 2,
        HISTORY = 0,
        CURRENT = 1
    };

    public class DocumentService : IDocumentService
    {
        private readonly IConfiguration _config;

        private readonly IDocumentStoreRepository<Document> _documentRepo;
        private readonly IDocumentStoreRepository<DocumentVersion> _documentVerRepo;
        private readonly IDocumentStoreRepository<DocumentType> _documentTypeRepo;
        private readonly IDocumentStoreRepository<DocumentAccess> _documentAccessRepo;
        private readonly IDocumentStoreRepository<RepriseDocument> _repriseDocRepo;
        private readonly IDocumentStoreRepository<User> _userService;

        private readonly object _blobLock;
        private BlobContainerClient _blobContainer;
        private readonly IMapper _mapper;

        public DocumentService(
            IConfiguration iconfig,
            IDocumentStoreRepository<Document> documenRepository,
            IDocumentStoreRepository<DocumentVersion> documentVerRepo,
            IDocumentStoreRepository<DocumentType> documentTypeRepo,
            IDocumentStoreRepository<DocumentAccess> documentAccessRepo,
            IDocumentStoreRepository<RepriseDocument> documentStoreRepository_RepriseDocuments,
            IDocumentStoreRepository<User> userService,
            IMapper mapper)
        {
            _documentRepo = documenRepository ?? throw new ArgumentNullException(nameof(documenRepository));
            _documentVerRepo = documentVerRepo ?? throw new ArgumentNullException(nameof(documentVerRepo));
            _repriseDocRepo = documentStoreRepository_RepriseDocuments ?? throw new ArgumentNullException(nameof(documentStoreRepository_RepriseDocuments));
            _documentTypeRepo = documentTypeRepo ?? throw new ArgumentNullException(nameof(documentTypeRepo));
            _documentAccessRepo = documentAccessRepo ?? throw new ArgumentNullException(nameof(documentAccessRepo));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            _config = iconfig ?? throw new ArgumentNullException(nameof(iconfig));

            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            _blobLock = new object();
        }

        #region Azure blob storage handling

        private async Task AssertBlobContainer()
        {
            
            if (this._blobContainer == null)
            {
                string connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

                // Create a BlobServiceClient object which will be used to create a container client
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

                //Create a unique name for the container
                string containerName = _config.GetSection("Azure").GetSection("BlobStorage").GetValue<string>("UserDocumentsContainer");

                lock (_blobLock)
                {
                    // Create the container and return a container client object
                    this._blobContainer = blobServiceClient.GetBlobContainerClient(containerName);
                }

                if (!(await this._blobContainer.ExistsAsync()))
                {
                    throw new ArgumentNullException($"Container { containerName } does not exists");
                }
            }
            
            if (this._blobContainer == null)
            {
                throw new FileNotFoundException("Blob Empty");
            }
        }

        private async Task<bool> UploadBlob(Guid blobGUID, string filename, byte[] binary)
        {
            if (blobGUID == null)
            {
                return false;
            }

            try
            {
                await AssertBlobContainer();

                BlobClient blobClient = _blobContainer.GetBlobClient($"{ blobGUID }");
                if (blobClient == null)
                {
                    throw new RequestFailedException($"Could not open a blob client for document { filename }");
                }

                using (var memoryStream = new MemoryStream(binary))
                {
                    await blobClient.UploadAsync(memoryStream, overwrite: true);
                }

                return true;
            }

            catch (Exception)
            {
                return false;
            }
        }

        private async Task<MemoryStream> DownloadBlob(Guid blobGuid)
        {
            try
            {
                await AssertBlobContainer();

                // Get a reference to a blob
                BlobClient blobClient = this._blobContainer.GetBlobClient($"{ blobGuid }");

                var blobDl = await blobClient.DownloadAsync();

                var stream = new MemoryStream();
                await blobDl.Value.Content.CopyToAsync(stream);

                return stream;
            }

            catch (Exception)
            {
                return null;
            }
        }

        private async Task<bool> DeleteBlob(Guid blobGUID)
        {
            try
            {
                await AssertBlobContainer();

                // Get a reference to a blob
                BlobClient blobClient = this._blobContainer.GetBlobClient($"{ blobGUID }");

                if (await blobClient.DeleteIfExistsAsync())
                {
                    throw new InvalidOperationException("Failed to remove from blob storage");
                }

                return true;
            }

            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region document CRUD

        #region List documents
        public async Task<IEnumerable<DocumentViewDTO>> ListById(string id, DocumentState docState, HashSet<string> docTypes = null)
        {
            try
            {
                if (!Guid.TryParse(id, out var potentialGuid))
                {
                    potentialGuid = Guid.NewGuid(); // Random Guid to prevent anomaly in the query
                }

                // if it is user, then get all sfIds
                var potentialUserSfIds = await _userService.Query()
                    .Include(u => u.UserSfLinkFkNav)
                    .Where(u => u.Login == id)
                    .Select(u => u.UserSfLinkFkNav.Select(s => s.SalesforceAccountId))
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (!(potentialUserSfIds ?? new List<string>()).Any())
                {
                    potentialUserSfIds = new List<string> { id };
                }

                var guidsFromOwnership = _documentAccessRepo.List(a => potentialUserSfIds.Contains(a.AllowedSfId)).AsNoTracking().Select(a => a.DocumentGUID).ToHashSet();

                var dtos = new List<DocumentViewDTO>();

                await _documentRepo.List(d => d.SfId == id || d.GUID == potentialGuid || guidsFromOwnership.Contains(d.GUID))
                    .Include(d => d.DocumentTypeFkNav)
                    .Include(d => d.DocumentVersionsFkNav)
                    .Include(d => d.DocumentAccessFkNav)
                    .Where(d => d.DocumentVersionsFkNav.Any())
                    .AsNoTracking()
                    .ForEachAsync(d =>
                    {
                        var dto = StoreDocumentViewToDto(d, docState, docTypes);
                        if (dto != null)
                        {
                            dtos.Add(dto);
                        }
                    });

                return dtos;
            }

            catch (Exception)
            {
                return null;
            }
        }

        private DocumentViewDTO StoreDocumentViewToDto(Document document, DocumentState docState, HashSet<string> docTypes)
        {
            var docType = document.DocumentTypeFkNav.Name;
            if (docTypes == null || docTypes.Count == 0 || docTypes.Contains(docType))
            {
                var docDTO = _mapper.Map<DocumentViewDTO>(document);
                docDTO.FileCategory = docType;

                var maxDate = document.DocumentVersionsFkNav.Aggregate((a, b) => a.Date > b.Date ? a : b).Date;

                docDTO.Versions = document.DocumentVersionsFkNav
                    .Where(v => (v.Date == maxDate && (docState != 0)) || (v.Date != maxDate && ((int)docState % 2 == 0)))
                    .OrderByDescending(v => v.Date)
                    .Select(v => _mapper.Map<DocumentVersionDTO>(v));

                return docDTO;
            }

            return null;
        }

        #endregion

        #region Upload actions


        public async Task<Tuple<int, IHttpMessage>> Create(DocumentDTO doc, byte[] binary, string[] authorizedSfIds = null, bool isPublic = false)
        {
            if (doc == null || binary == null || binary.Length == 0)
            {
                return Tuple.Create(400, new HttpErrorMessage("document details and/or binary content are mandatory") as IHttpMessage);
            }

            if (string.IsNullOrEmpty(doc.FileName))
            {
                return Tuple.Create(400, new HttpErrorMessage("document must have a name") as IHttpMessage);
            }

            if ((authorizedSfIds == null || authorizedSfIds.Length == 0) && !isPublic)
            {
                return Tuple.Create(400, new HttpErrorMessage("you must have at least one user who has full control access") as IHttpMessage);
            }

            var document = await CreateInitialDocument(doc);

            var initialVersion = CreateNewDocumentVersion(document, doc, binary);

            document = AddAccessesToInitialDocument(document, authorizedSfIds, isPublic);
            if (document == null)
            {
                Tuple.Create(500, new HttpErrorMessage("Properties to make this document public is not found in server configuration") as IHttpMessage);
            }
            

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                if (await _documentRepo.Add(document) == null)
                {
                    return Tuple.Create(500, new HttpErrorMessage("Failed to upload document") as IHttpMessage);
                }

                if (await _documentVerRepo.Add(initialVersion) == null)
                {
                    return Tuple.Create(500, new HttpErrorMessage("Failed to save initial version details of the document") as IHttpMessage);
                }

                if (!await UploadBlob(initialVersion.BlobGUID, document?.FileName, binary))
                {
                    return Tuple.Create(500, new HttpErrorMessage("Failed to upload the binary content to server") as IHttpMessage);
                }

                scope.Complete();
            }

            return Tuple.Create(200, new HttpSuccessMessage("Document successfully uploaded") as IHttpMessage);
        }

        private async Task<Document> CreateInitialDocument(DocumentDTO doc)
        {
            doc.GUID = Guid.NewGuid();

            var document = _mapper.Map<Document>(doc);

            try
            {
                var docType = await _documentTypeRepo.List(t => t.Name == (doc.FileCategory ?? string.Empty)).AsNoTracking().FirstOrDefaultAsync();
                if (docType == null)
                {
                    return null;
                }

                document.FileCategory = docType.ID;
            }

            catch (Exception)
            {
                return null;
            }

            return document;
        }

        private Document AddAccessesToInitialDocument(Document document, string[] authorizedSfIds, bool isPublic)
        {
            if (document.DocumentAccessFkNav == null)
            {
                document.DocumentAccessFkNav = new List<DocumentAccess>();
            }

            if (authorizedSfIds != null && !isPublic)
            {
                foreach (var sfId in authorizedSfIds)
                {
                    var access = new DocumentAccess
                    {
                        DocumentGUID = document.GUID,
                        AllowedSfId = sfId
                    };

                    document.DocumentAccessFkNav.Add(access);
                }
            }

            else if (isPublic)
            {
                try
                {
                    var access = new DocumentAccess
                    {
                        DocumentGUID = document.GUID,
                        AllowedSfId = _config.GetSection("DocumentProperties").GetValue<string>("MakeDocumentPublicAccessKey")
                    };

                    document.DocumentAccessFkNav.Add(access);
                }

                catch (Exception)
                {
                    return null;
                }
            }

            return document;
        }

        public async Task<Tuple<int, IHttpMessage>> Update(DocumentDTO doc, byte[] binary, string ownership="", bool isCallerAdmin=false)
        {
            var existingDocument = await _documentRepo.List(d => d.GUID == doc.GUID).AsNoTracking().FirstOrDefaultAsync();
            if (existingDocument == null)
            {
                return Tuple.Create(404, new HttpErrorMessage("No such document exists for upload") as IHttpMessage);
            }

            var hasAccess = isCallerAdmin || await _documentAccessRepo.List(a => 
                (a.AllowedSfId == ownership && !string.IsNullOrEmpty(ownership) && 
                a.DocumentGUID == doc.GUID)
            ).AsNoTracking().AnyAsync();

            if (!hasAccess)
            {
                return Tuple.Create(403, new HttpErrorMessage("You don't have the right to update this document") as IHttpMessage);
            }

            var hashSum = HashCheckSumDocument(binary);

            var latestKnownVersionHashSum = await _documentVerRepo
                .List(v => v.DocumentGUID == doc.GUID)
                .OrderByDescending(v => v.Date).AsNoTracking()
                .Select(v => v.CheckSum).FirstOrDefaultAsync();

            if (hashSum == latestKnownVersionHashSum && doc.FileName == existingDocument.FileName)
            {
                return Tuple.Create(200, new HttpSuccessMessage("No update is needed for this document") as IHttpMessage);
            }

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                if (existingDocument.FileName != doc.FileName)
                {
                    existingDocument.FileName = doc.FileName;
                    _documentRepo.Attach(existingDocument);

                    if (!await _documentRepo.Update(existingDocument))
                    {
                        return Tuple.Create(500, new HttpErrorMessage("Failed to rename the document (before update)") as IHttpMessage);
                    }
                }

                if (hashSum != latestKnownVersionHashSum && !await AppendNewDocumentVersionToBlobDB(existingDocument, doc, binary))
                {
                    return Tuple.Create(500, new HttpErrorMessage("Failed to update to server the new version of the document") as IHttpMessage);
                }

                scope.Complete();
            }

            return Tuple.Create(200, new HttpSuccessMessage("Document has been updated") as IHttpMessage);
        }

        private async Task<bool> AppendNewDocumentVersionToBlobDB(Document document, DocumentDTO doc, byte[] binary)
        {

            var newVersion = CreateNewDocumentVersion(document, doc, binary);
            if ((await _documentVerRepo.Add(newVersion)) == null)
            {
                return false;
            }

            return await UploadBlob(newVersion.BlobGUID, doc.FileExtension, binary);
        }

        private DocumentVersion CreateNewDocumentVersion(Document document, DocumentDTO dto, byte[] binary)
        {
            return new DocumentVersion
            {
                BlobGUID = Guid.NewGuid(),
                DocumentGUID = document.GUID,
                CheckSum = HashCheckSumDocument(binary),
                Date = DateTime.Now,
                FileExtension = dto.FileExtension,
                FileSize = binary.Length,
                ModifiedBy = (string.IsNullOrEmpty(dto.ModifiedBy)) ? "Undefined" : dto.ModifiedBy
            };
        }

        private string HashCheckSumDocument(byte[] binary)
        {
            if (binary == null || binary.Length == 0)
            {
                return null;
            }

            using (SHA256 sha = SHA256.Create())
            {
                return Convert.ToBase64String(sha.ComputeHash(binary));
            }
        }

        #endregion

        public async Task<DownloadDocumentDTO> Download(string userSfId, Guid blobGUID)
        {
            var doc = await _documentVerRepo.List(v => v.BlobGUID == blobGUID).AsNoTracking().FirstOrDefaultAsync();
            if (doc == null || doc.DocumentGUID == Guid.Empty)
            {
                return new DownloadDocumentDTO
                {
                    HttpResponseCode = 404,
                    HttpResponseMessage = "No such blob exists in the GED"
                };
            }

            var docActive = await _documentRepo.List(d => d.GUID == doc.DocumentGUID).AsNoTracking().FirstOrDefaultAsync();
            if (docActive == null)
            {
                return new DownloadDocumentDTO
                {
                    HttpResponseCode = 404,
                    HttpResponseMessage = "Cannot download a deleted/inexistant document"
                };
            }

            string publicAccessKey;
            try
            {
                publicAccessKey = _config.GetSection("DocumentProperties").GetValue<string>("MakeDocumentPublicAccessKey");
            }

            catch (Exception)
            {
                return new DownloadDocumentDTO
                {
                    HttpResponseCode = 500,
                    HttpResponseMessage = "Properties to make this document public is not found in server configuration"
                };
            }

            var userHasAccess = await _documentAccessRepo.List(a => 
                (a.AllowedSfId == userSfId && !string.IsNullOrEmpty(userSfId) || a.AllowedSfId == publicAccessKey) &&
                a.DocumentGUID == doc.DocumentGUID
            ).AsNoTracking().AnyAsync();

            if (!userHasAccess)
            {
                return new DownloadDocumentDTO
                {
                     HttpResponseCode = 403,
                     HttpResponseMessage = "You do not have the rights to this access this document"
                };
            }

            var stream = await DownloadBlob(blobGUID);
            if (stream == null)
            {
                return new DownloadDocumentDTO
                {
                    HttpResponseCode = 500,
                    HttpResponseMessage = "Failed to retrieve the document from GED"
                };
            }

            return new DownloadDocumentDTO
            {
                HttpResponseCode = 200,
                StreamContent = stream,
                FileName = docActive.FileName,
                FileExtension = doc.FileExtension
            };
        }

        public async Task<Tuple<int, IHttpMessage>> Delete(string ownership, Guid? guid = null, string sfId = null, bool ownershipIgnored = false)
        {
            if (guid == null && string.IsNullOrEmpty(sfId))
            {
                return Tuple.Create(200, new HttpSuccessMessage("No action taken") as IHttpMessage);
            }

            if (!string.IsNullOrEmpty(sfId) || guid == null)
            {
                guid = Guid.NewGuid(); // There is a negligeable chance that Guid is available in the database
            }

            try
            {
                var existingDoc = await _documentRepo.List(d => d.GUID == guid || (d.SfId == sfId && !string.IsNullOrEmpty(sfId))).FirstOrDefaultAsync();
                if (existingDoc == null)
                {
                    return Tuple.Create(404, new HttpErrorMessage("No such file exists") as IHttpMessage);
                }

                if (!ownershipIgnored)
                {
                    var hasAccess = await _documentAccessRepo.List(a => a.AllowedSfId == ownership && a.DocumentGUID == existingDoc.GUID).AsNoTracking().AnyAsync();
                    if (!hasAccess)
                    {
                        return Tuple.Create(403, new HttpErrorMessage("You do not have the right to delete this document") as IHttpMessage);
                    }
                }

                await _documentVerRepo.List(v => v.DocumentGUID == existingDoc.GUID).AsNoTracking().Select(v => v.BlobGUID)
                    .ForEachAsync(async b => {
                        await DeleteBlob(b);
                    });
                
                if (!await _documentRepo.Delete(existingDoc))
                {
                    return Tuple.Create(500, new HttpErrorMessage("Error deleting in blob server, but it will not be referenced to the user") as IHttpMessage);
                }
            }

            catch (Exception)
            {
                return Tuple.Create(500, new HttpErrorMessage("Unexpected error occured while deleting doc") as IHttpMessage);
            }

            return Tuple.Create(200, new HttpSuccessMessage("Document deleted successfully") as IHttpMessage);
        }

        #region document access management
        public async Task<Tuple<int, IHttpMessage>> AddDocAccesses(DocumentDTO doc, string[] sfIds)
        {
            return await AddOrRemoveDocAccesses(doc, sfIds);
        }

        public async Task<Tuple<int, IHttpMessage>> RemoveDocAccesses(DocumentDTO doc, string[] sfIds)
        {
            return await AddOrRemoveDocAccesses(doc, sfIds, false);
        }

        private async Task<Tuple<int, IHttpMessage>> AddOrRemoveDocAccesses(DocumentDTO doc, string[] sfIds, bool isActionAdd = true)
        {
            if (doc.GUID == null)
            {
                return Tuple.Create(400, new HttpErrorMessage("No GUID has been given.") as IHttpMessage);
            }

            var document = await _documentRepo.List(d => d.GUID == doc.GUID || d.SfId == doc.SfId).AsNoTracking().FirstOrDefaultAsync();
            if (document == null)
            {
                return Tuple.Create(400, new HttpErrorMessage("Document doesn't exists.") as IHttpMessage);
            }

            if (sfIds == null || sfIds.Length == 0)
            {
                return Tuple.Create(200, new HttpSuccessMessage("No action taken") as IHttpMessage);
            }

            var sfIdsHashSet = new HashSet<string>(sfIds);

            await _documentAccessRepo.List(
                a => 
                    a.DocumentGUID == document.GUID && 
                    isActionAdd == sfIdsHashSet.Contains(a.AllowedSfId)
            ).AsNoTracking().ForEachAsync(
                a => 
                    sfIdsHashSet.Remove(a.AllowedSfId)
            );

            var newAccesses = new List<DocumentAccess>();
            foreach (var sfId in sfIdsHashSet)
            {
                newAccesses.Add(new DocumentAccess
                {
                    AllowedSfId = sfId,
                    DocumentGUID = (Guid)doc.GUID
                });
            }

            var isSuccess = await ((isActionAdd) ? _documentAccessRepo.AddRange(newAccesses) : _documentAccessRepo.DeleteRange(newAccesses));
            if (!isSuccess)
            {
                return Tuple.Create(500, new HttpErrorMessage("Something went wrong when addin accesses") as IHttpMessage);
            }

            return Tuple.Create(200, new HttpSuccessMessage("New accesses has been removed successfully") as IHttpMessage);
        }

        #endregion

        #endregion

        #region document types CRUD

        public async Task<IEnumerable<DocumentTypeDTO>> ListAllTypes()
        {
            try
            {
                var types = await _documentTypeRepo.Query().AsNoTracking().ToListAsync();
                var dtos = new List<DocumentTypeDTO>();

                foreach (var type in types)
                {
                    var dto = _mapper.Map<DocumentTypeDTO>(type);
                    dtos.Add(dto);
                }

                return dtos;
            }

            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<DocumentTypeDTO> GetTypeInfo(int id)
        {
            try
            {
                var docType = await _documentTypeRepo.List(t => t.ID == id).AsNoTracking().ToListAsync();
                if (docType == null)
                {
                    return null;
                }

                return _mapper.Map<DocumentTypeDTO>(docType);
            }

            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Reprise documentaire

        public async Task<bool> AddRepriseDocumentsToDedicatedTable(IEnumerable<RepriseDocument> documents)
        {
            try
            {
                return await _repriseDocRepo.AddRange(documents);
            }

            catch (Exception)
            {
                return false;
            }
        }

        #endregion
    }
}
