using Azure.Api.Data.DTOs;
using System;
using System.Threading.Tasks;

namespace Azure.Api.Services
{
    public interface IIntranetViewerService
    {
        Task<Tuple<int, IHttpMessage>> ListLpcrClients(string search, int page, int pageSize);
        Task<Tuple<int, IHttpMessage>> ListLpcrRepriseDocuments(int page, int pageSize);
        Task<Tuple<int, IHttpMessage>> DownloadLpcrRepriseDocument(string path);
        Task<Tuple<int, IHttpMessage>> TestCommunicationWithLpcrApi();
    }
}
