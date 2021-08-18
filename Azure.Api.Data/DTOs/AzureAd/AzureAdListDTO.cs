using Azure.Api.Data.DTOs.AzureAd;
using System.Collections.Generic;
using System.Security.Claims;


namespace Azure.Api.Data.DTOs
{
    public class AzureAdListDTO
    {
        public int TotalUser { get; set; }
        public IEnumerable<AzureAdUserDTO> PageResult { get; private set; }

        public AzureAdListDTO(int totalUser, IEnumerable<AzureAdUserDTO> pageResult)
        {
            TotalUser = totalUser;
            PageResult = pageResult;
        }
    }
}


