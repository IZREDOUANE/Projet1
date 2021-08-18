using Azure.Api.Data.DTOs.Account;
using Azure.Api.Data.DTOs.SalesForce;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Azure.Api.Services
{
    public class AccountCrecheService: IAccountCrecheService
    {
        private readonly ISalesForceService _salesforceService;

        public AccountCrecheService(ISalesForceService serviceSalesForce)
        {
            this._salesforceService = serviceSalesForce;
        }

        /// <summary>
        /// Get creche of the director using its sfId
        /// </summary>
        /// <param name="director">sfId of the director</param>
        /// <returns>a creche object</returns>
        public async Task<List<AccountCrecheDTO>> GetDirectorCrecheNameAndIdAsync(string director)
        {
            var creches = new List<AccountCrecheDTO>();
            var crechesSF = await _salesforceService.GetDirectorCrecheNameAndIdAsync(director);
            foreach (var crecheSF in crechesSF)
            {
                creches.Add(new AccountCrecheDTO
                {
                    Name = crecheSF.Name,
                    sfId = crecheSF.Id
                });
            }

            return creches;
        }

        public async Task<List<AccountCrecheDTO>> GetallCrechesNameAndIdAsync()
        {
            var creches = new List<AccountCrecheDTO>();
            var crechesSF = await _salesforceService.GetCrecheNameAndIdListAsync();
            foreach (var crecheSF in crechesSF)
            {
                creches.Add(new AccountCrecheDTO
                {
                    Name = crecheSF.Name,
                    sfId = crecheSF.Id
                });
            }

            return creches;
        }


        /// <summary>
        /// Gets the infos of a creche based on a parentId of a user
        /// </summary>
        /// <param name="parentID">sfId of a espace_famille user</param>
        /// <returns>list of creches</returns>
        public async Task<List<AccountCrecheDTO>> GetCrecheInfoByIdAsync(string parentID)
        {
            return await this.GetCrecheInfoGeneric(
                async () => await this._salesforceService.GetAccountCrecheByUserIdAsync(parentID)
            );
        }

        /// <summary>
        /// Gets the infos of a creche based on a creche Id of a user
        /// </summary>
        /// <param name="crecheID">sfId of a creche</param>
        /// <returns>a creche object</returns>
        public async Task<AccountCrecheDTO> GetCrecheInfoByCrecheSfIdAsync(string crecheID)
        {
            AccountCrecheDTO creche = null;

            try
            {
                var crecheSF = await this._salesforceService.GetAccountCrecheByCrecheSFId(crecheID);
                creche = new AccountCrecheDTO
                {
                    sfId = crecheSF.Id,
                    Name = crecheSF.Name,
                    Phone = crecheSF.Phone,
                    Email = crecheSF.Email__c,
                    BerceauxCommercialisable = crecheSF.Lpcr_BerceauxCommercialisables__c,
                    BerceauxDisponibles = crecheSF.LPCR_BerceauxDisponibles__c,
                    ShippingAddress = crecheSF.ShippingAddress,
                    ContactDirectrice = crecheSF.LPCR_ContactDirectrice__r,
                    ServiceFamille = crecheSF.LPCR_InterlocuteurServiceFamille__r
                };
            }

            catch (Exception)
            {
                return null;
            }

            return creche;
        }

        private async Task<List<AccountCrecheDTO>> GetCrecheInfoGeneric(Func<Task<List<AccountCrecheSalesForceDTO>>> sfServiceFunc)
        {
            var creches = new List<AccountCrecheDTO>();
            try
            {
                var crechesSF = await sfServiceFunc();

                if (crechesSF == null)
                {
                    throw new Exception();
                }

                foreach (var crecheSF in crechesSF)
                {
                    creches.Add(new AccountCrecheDTO
                    {
                        sfId = crecheSF.Id,
                        Name = crecheSF.Name,
                        Phone = crecheSF.Phone,
                        Email = crecheSF.Email__c,
                        BerceauxCommercialisable = crecheSF.Lpcr_BerceauxCommercialisables__c,
                        BerceauxDisponibles = crecheSF.LPCR_BerceauxDisponibles__c,
                        ShippingAddress = crecheSF.ShippingAddress,
                        ContactDirectrice = crecheSF.LPCR_ContactDirectrice__r,
                        ServiceFamille = crecheSF.LPCR_InterlocuteurServiceFamille__r
                    });
                }
            }

            catch (Exception)
            {
                return null;
            }

            return creches;
        }

        public async Task<List<AccountSalesForceDTO>> GetListOfFamiliesFromCreche(string crecheId)
        {
            return await this._salesforceService.GetAccountIdOfContractsFromCreche(crecheId);
        }
    }
}
