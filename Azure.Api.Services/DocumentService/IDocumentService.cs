using Azure.Api.Data.DTOs;
using Azure.Api.Data.DTOs.Document;
using Azure.Api.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Azure.Api.Services
{
    public interface IDocumentService
    {
        /// <summary>
        /// Lists the documents available in the GED based on the id inserted by the caller
        /// </summary>
        /// <param name="id">could be either the document GUID, the document SfID or an account SfId</param>
        /// <param name="docState">could be either CURRENT(1), ANY(2) or HISTORY(0)</param>
        /// <param name="docTypes">a list of doctype names that the user wish to filter</param>
        /// <returns>a payload of the search result</returns>
        Task<IEnumerable<DocumentViewDTO>> ListById(string id, DocumentState docState, HashSet<string> docTypes = null);

        /// <summary>
        /// Creates a new document in the GED 
        /// </summary>
        /// <param name="doc">details of the document (GUID is ignored)</param>
        /// <param name="binary">content of the document</param>
        /// <param name="authorizedSfIds">account sfIds that are allowed to have access to this document</param>
        /// <param name="isPublic">boolean to make the document accessible for everyone (overwrites the authorized sfIds if true)</param>
        /// <returns>HTTP code and HTTP message</returns>
        Task<Tuple<int, IHttpMessage>> Create(DocumentDTO doc, byte[] binary, string[] authorizedSfIds = null, bool isPublic = false);

        /// <summary>
        /// Updates an existing document in the GED
        /// </summary>
        /// <param name="doc">details of the document (GUID is mandatory)</param>
        /// <param name="binary">content of the document</param>
        /// <param name="ownership">the user sfId that wants to update</param>
        /// <param name="isCallerAdmin">the user calling this method is admin or not (overwrite ownership if true)</param>
        /// <returns>HTTP code and HTTP message</returns>
        Task<Tuple<int, IHttpMessage>> Update(DocumentDTO doc, byte[] binary, string ownership = "", bool isCallerAdmin = false);

        /// <summary>
        /// Deletes an existing document along its history
        /// </summary>
        /// <param name="ownership">the user sfId that wants to update</param>
        /// <param name="guid">GUID of the document the caller wish to delete</param>
        /// <param name="sfId">SfId of the document (GUID ignored if SfId is filled)</param>
        /// <param name="ownershipIgnored">is the ownership verification ignored before delete?</param>
        /// <returns>>HTTP code and HTTP message</returns>
        Task<Tuple<int, IHttpMessage>> Delete(string ownership, Guid? guid = null, string sfId = null, bool ownershipIgnored = false);

        /// <summary>
        /// Downloads the document from the GED
        /// </summary>
        /// <param name="userSfId">the user sfId that wants to download</param>
        /// <param name="blobGUID">the blob GUID of the version of a document</param>
        /// <returns>
        ///     a payload containing:
        ///         - A HTTP code
        ///         - A HTTP message (if error)
        ///         - The content of the document selected (if success)
        ///         - name and extension of the document (if success)
        /// </returns>
        Task<DownloadDocumentDTO> Download(string userSfId, Guid blobGUID);

        /// <summary>
        /// Get all the infos of a type of document in the GED
        /// </summary>
        /// <param name="id">id of the document type</param>
        /// <returns>info details of the document type</returns>
        Task<DocumentTypeDTO> GetTypeInfo(int id);

        /// <summary>
        /// Get all the types available from the GED
        /// </summary>
        /// <returns>a list of info details of the document type</returns>
        Task<IEnumerable<DocumentTypeDTO>> ListAllTypes();

        Task<bool> AddRepriseDocumentsToDedicatedTable(IEnumerable<RepriseDocument> documents);
    }
}
