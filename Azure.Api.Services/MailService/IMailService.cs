using Azure.Api.Data.DTOs;
using Azure.Api.Data.DTOs.Email;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Azure.Api.Services
{
    public interface IMailService
    {
        public Task<bool> SendMail(EmailDTO email, IEnumerable<EmailDocumentDTO> documents = null);
    }
}
