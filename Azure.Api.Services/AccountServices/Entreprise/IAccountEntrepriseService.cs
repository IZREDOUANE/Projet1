using Azure.Api.Data.DTOs;
using System;
using System.Collections.Generic;
using System.Text;
using Azure.Api.Data.Models;
using System.Threading.Tasks;
using Azure.Api.Data.DTOs.Account;
using Azure.Api.Data.DTOs.Contact;

namespace Azure.Api.Services.Entreprise
{
    /// <summary>
    /// Interface Fadaa charika
    /// </summary>
    public interface IAccountEntrepriseService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accountSfId"></param>
        /// <returns></returns>
        public Task<IEnumerable<ContactLpcrDTO>> GetContactsLpcr(string accountSfId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accountSfId"></param>
        /// <returns></returns>
        public Task<AccountEntrepriseDTO> GetAccountInfos(string accountSfId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sfId"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        public Task<bool> UpdateAccountEntreprise(string sfId, AccountEntrepriseDTO account);
    }
}
