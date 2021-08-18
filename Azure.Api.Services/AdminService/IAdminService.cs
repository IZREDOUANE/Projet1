using Azure.Api.Data.DTOs;
using Azure.Api.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Azure.Api.Services
{
    public interface IAdminService
    {
        public Task<Admin> GetAdminByGUID(string GUID);
        public Task<AdminAccess> GetAdminAccess(string adGUID, string platform);
    }
}
