using Azure.Api.Data.DTOs;
using Azure.Api.Data.DTOs.Account;
using Azure.Api.Data.DTOs.SalesForce;
using Salesforce.Common.Models.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Api.Data.DTOs.Preinscription;
using Azure.Api.Data.Models;

namespace Azure.Api.Services
{
    public interface ISalesForceService
    {
        public Task<string> GetRecordTypeNameAsync(string Id);

        public Task<string> GetAccountIdByEmailAsync(string email);

        public Task<bool> IsContactExistAsync(string email);

        public Task<SuccessResponse> CreateAccountFamilleAsync(AccountFamilleDTO account);

        public Task<SuccessResponse> CreateContactPEAsync(string sfId, ContactParentEnfantDTO newContact);

        public Task<bool> DeleteContactAsync(string contactID);

        public Task<List<ContractSalesForceDTO>> GetCrechesIdOfContractsFromUserIdAsync(string id);

        public Task<List<AccountSalesForceDTO>> GetAccountIdOfContractsFromCreche(string id);

        public Task<List<ContactParentEnfantSalesForceDTO>> GetContactsPEFromSFIDAsync(string sfID);

        public Task<QueryResult<AccountSalesForceDTO>> GetSfAccount(string id, params string [] fields);

        public Task<AccountSalesForceDTO> GetAllAccountFamilleInformationAsync(string id);

        public Task<bool> UpdateAccountEntrepriseDetails(string sfId, AccountEntrepriseDTO account);

        public Task<bool> UpdateAccountFamilyInformationDetails(string sfId, AccountFamilleDTO account);

        public Task<List<AccountCrecheSalesForceDTO>> GetDirectorCrecheNameAndIdAsync(string director);

        public Task<List<AccountCrecheSalesForceDTO>> GetCrecheNameAndIdListAsync();

        public Task<List<AccountCrecheSalesForceDTO>> GetAccountCrecheByUserIdAsync(string id);

        public Task<AccountCrecheSalesForceDTO> GetAccountCrecheByCrecheSFId(string id);

        public Task<string> CreateContactAsync(ContactSalesForceDTO contact);

        public Task<string> CreateBasicAccountAsync(AccountSalesForceDTO account);

        public AccountSalesForceDTO MapRegisterFamilleDTO_to_AccountSalesForceDTO (RegisterFamilleDTO account);

        public ContactParentSalesForceDTO MapRegisterFamilleDTO_to_ContactSalesforceParent(RegisterFamilleDTO account, int ordreParent);

        public  Task<string> CreateAccountSalesforceAsync(RegisterFamilleDTO account);

        public Task<string> GetCollaborateurSfIdFromEmailAsync(string email);

        Task<List<PreinscriptionStatusDto>> GetAllStatus();
        Task<List<PreinscriptionInfoDto>> GetPreinscriptionsBy(string key, bool? filtered);

        Task<SuccessResponse> UpdatePreinscriptionsById(string preinscriptionId, PreinscriptionUpdateDto statusDto);
        Task<IEnumerable<KeyValuePair<string, string>>> FindAllPreinscriptionIdBy(string sfId, string accountId);
        Task<AccountContactsLpcrSalesForceDTO> GetContactsLpcrFromAccount(string accountSfId);
    }
}
