using Azure.Api.Data.DTOs;
using Azure.Api.Data.Models;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Azure.Api.Data.DTOs.SalesForce;
using Azure.Api.Data.DTOs.Contact;
using System.Collections.Generic;
using System.Linq;

namespace Azure.Api.Services.Entreprise
{
    /// <summary>
    /// Service des Comptes Entreprises
    /// </summary>
    public class AccountEntrepriseService : IAccountEntrepriseService
    {
        public IConfiguration _configuration;
        private ISalesForceService _sfService;

        /// <summary>
        /// Constructeur
        /// </summary>
        /// <param name="userService"></param>
        /// <param name="salesForceService"></param>
        public AccountEntrepriseService(
            IConfiguration configuration, 
            ISalesForceService sfService
        )
        {
            _configuration = configuration;
            _sfService = sfService;
        }

        public async Task<AccountEntrepriseDTO> GetAccountInfos(string accountSfId)
        {
            var fields = new string[]
            {
                "Name",
                "Email__c",
                "Phone"
            };

            var queryResults = await _sfService.GetSfAccount(accountSfId, fields);
            var sfAccount = queryResults.Records.FirstOrDefault();

            return MapsfAccountEntToApiDTO(sfAccount);
        }

        public async Task<bool> UpdateAccountEntreprise(string sfId, AccountEntrepriseDTO account)
        {
            return await _sfService.UpdateAccountEntrepriseDetails(sfId, account);
        }

        public AccountEntrepriseDTO MapsfAccountEntToApiDTO(AccountSalesForceDTO account)
        {
            if (account == null)
            {
                return null;
            }

            return new AccountEntrepriseDTO
            {
                NomComplet = account.Name,
                Login = account.Email__c,
                Phone = account.Phone
            };
        }

        public async Task<IEnumerable<ContactLpcrDTO>> GetContactsLpcr(string accountSfId)
        {
            var sfContactsLpcr = await _sfService.GetContactsLpcrFromAccount(accountSfId);
            if (sfContactsLpcr == null)
            {
                return null;
            }

            return MapContactsLpcrSFToApiDTO(sfContactsLpcr);
        }

        private IEnumerable<ContactLpcrDTO> MapContactsLpcrSFToApiDTO(AccountContactsLpcrSalesForceDTO contacts)
        {
            var payload = new List<ContactLpcrDTO>();

            payload.Add(
                MapContactLpcrSFToApiDTO("Responsable ADV", contacts.LPCR_ResponsableADV__r)
            );

            payload.Add(
                MapContactLpcrSFToApiDTO("Responsable Service Famille", contacts.LPCR_ResponsableServiceFamille__r)
            );

            payload.Add(
                MapContactLpcrSFToApiDTO("Commercial(e)", contacts.Owner)
            );

            return payload;
        }

        private ContactLpcrDTO MapContactLpcrSFToApiDTO(string type, UserSalesForceDTO contact)
        {
            return new ContactLpcrDTO
            {
                Type = type,
                FirstName = contact?.Firstname ?? string.Empty,
                LastName = contact?.LastName ?? string.Empty,
                Email = contact?.Email ?? string.Empty,
                MobilePhone = contact?.MobilePhone ?? string.Empty,
                Phone = contact?.Phone ?? string.Empty
            };
        }
    }
}
