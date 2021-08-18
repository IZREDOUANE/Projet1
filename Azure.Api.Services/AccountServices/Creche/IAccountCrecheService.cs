using Azure.Api.Data.DTOs.Account;
using Azure.Api.Data.DTOs.SalesForce;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Azure.Api.Services
{
    public interface IAccountCrecheService
    {
        public Task<List<AccountCrecheDTO>> GetDirectorCrecheNameAndIdAsync(string director);

        public Task<List<AccountCrecheDTO>> GetallCrechesNameAndIdAsync();

        public Task<List<AccountCrecheDTO>> GetCrecheInfoByIdAsync(string parentID);

        public Task<AccountCrecheDTO> GetCrecheInfoByCrecheSfIdAsync(string crecheID);

        public Task<List<AccountSalesForceDTO>> GetListOfFamiliesFromCreche(string crecheId);
    }
}
