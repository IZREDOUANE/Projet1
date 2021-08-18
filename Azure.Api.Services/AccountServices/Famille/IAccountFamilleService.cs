using Azure.Api.Data.DTOs.Account;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Azure.Api.Services
{
    public interface IAccountFamilleService
    {
        public Task<ContactParentEnfantDTO> CreateAccountContactPE(string sfID, ContactParentEnfantDTO contact);

        public Task<bool> DeleteContactPE(string sfId, ContactParentEnfantDTO contact);

        public Task<List<ContactParentEnfantDTO>> GetContactsBySFId(string sfID);

        public Task<AccountFamilleDTO> GetAccountFamilleBySfId(string sfID);

        public Task<AccountFamilleDTO> GetAccountFamilleInformationDetailsBySfId(string sfID);

        public Task<bool> UpdateAccountFamilyInformationDetails(string sfId, AccountFamilleDTO account);
    }
}
