using System;
using Azure.Api.Data.Models;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Azure.Api.Repository;
using Microsoft.EntityFrameworkCore;

namespace Azure.Api.Services
{
    public class AdminService : IAdminService
    {
        private readonly IDocumentStoreRepository<Admin> _adminRepository;

        private readonly IDocumentStoreRepository<AdminAccess> _adminAccesRepository;

        public AdminService(IDocumentStoreRepository<Admin> adminRepository, IDocumentStoreRepository<AdminAccess> adminAccesRepository)
        {
            _adminRepository = adminRepository ?? throw new ArgumentNullException(nameof(adminRepository));
            _adminAccesRepository = adminAccesRepository ?? throw new ArgumentNullException(nameof(adminAccesRepository));
        }


        // Defines the permission scopes used by the app
        public readonly static string[] Scopes =
        {
            "User.Read",
            //"MailboxSettings.Read",
            //"Calendars.ReadWrite"
        };

        public async Task<Admin> GetAdminByGUID(string GUID)
        {
            if (GUID is null)
            {
                throw new ArgumentNullException(nameof(GUID));
            }

            Admin admin = await GetAdmin(a => a.ADGUID == GUID);
            return admin;
        }

        public async Task<AdminAccess> GetAdminAccess(string adGUID, string platform)
        {
            if (string.IsNullOrEmpty(platform))
            {
                return null;
            }

            try
            {
                Expression<Func<AdminAccess, bool>> predicate =
                    aa => aa.AdminFkNav.ADGUID == adGUID
                    && (aa.PlatformFkNav.DomainName == platform);

                return await _adminAccesRepository.List(predicate)
                    .Include(aa => aa.AdminFkNav)
                    .Include(aa => aa.PlatformFkNav)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
            }

            catch (Exception)
            {
                return null;
            }
        }

        private async Task<Admin> GetAdmin(Expression<Func<Admin, bool>> predicate)
        {
            try
            {
                return await _adminRepository.GetByAsync(predicate);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
