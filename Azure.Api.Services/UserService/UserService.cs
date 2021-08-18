using AutoMapper;
using Azure.Api.Data.DTOs;
using Azure.Api.Data.Models;
using Azure.Api.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Transactions;

namespace Azure.Api.Services
{
    public enum PlatformValueType
    {
        DOMAIN_NAME = 0,
        APP_NAME = 1
    };

    public class UserService : IUserService
    {
        private readonly IMapper _mapper;
        private readonly IDocumentStoreRepository<User> _userRepo;
        private readonly IDocumentStoreRepository<UserAccess> _userAccessRepo;
        private readonly IDocumentStoreRepository<Platform> _platformRepo;
        private readonly IDocumentStoreRepository<UserSalesforceLink> _userSfLinkRepo;

        public UserService(
            IDocumentStoreRepository<User> userRepo, 
            IDocumentStoreRepository<UserAccess> userAccessRepo,
            IDocumentStoreRepository<Platform> platformRepo,
            IDocumentStoreRepository<UserSalesforceLink> userSfLinkRepo,
            IMapper mapper
        )
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _userAccessRepo = userAccessRepo ?? throw new ArgumentNullException(nameof(userAccessRepo));
            _platformRepo = platformRepo ?? throw new ArgumentNullException(nameof(platformRepo));
            _userSfLinkRepo = userSfLinkRepo ?? throw new ArgumentNullException(nameof(userSfLinkRepo));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        }

        public async Task<User> UpsertUser<T>(T user, string[] sfAccounts = null, string[] platforms = null)
        {
            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
                {
                    var mappedUser = _mapper.Map<User>(user);
                    var existingUser = await GetUser(mappedUser.Login);

                    if (existingUser == null)
                    {
                        mappedUser.ConfirmEmailToken = Guid.NewGuid();

                        var newUser = await _userRepo.Add(mappedUser);

                        if (newUser == null)
                        {
                            return null;
                        }

                        mappedUser = newUser;
                    }

                    else
                    {
                        mappedUser.GUID = existingUser.GUID;
                        if ( !(await _userRepo.Update(mappedUser)))
                        {
                            return null;
                        }
                    }

                    if (sfAccounts != null)
                    {
                        if (!(await DeleteUserAllSfLinks(mappedUser.GUID)))
                        {
                            return null;
                        }

                        if (!(await AddUserSfLink(mappedUser.GUID, sfAccounts)))
                        {
                            return null;
                        }
                    }

                    if (platforms != null)
                    {
                        if ( !(await DeleteUserAllAccesses(mappedUser.GUID)) )
                        {
                            return null;
                        }

                        if ( !(await AddUserAccess(mappedUser.GUID, platforms)))
                        {
                            return null;
                        }
                    }

                    scope.Complete();

                    return mappedUser;
                }
            }

            catch (Exception)
            {
                return null;
            }
        }

        #region sf links CRUD

        #region Delete sf link 
        public async Task<bool> DeleteUserSfLink(Guid? ID, string sfAccount)
        {
            return await DeleteUserSfLink(u => u.GUID == ID, sfAccount);
        }

        public async Task<bool> DeleteUserSfLink(string login, string sfAccount)
        {
            return await DeleteUserSfLink(u => u.Login == login, sfAccount);
        }

        public async Task<bool> DeleteUserSfLink(Expression<Func<User, bool>> predicate, string sfAccount)
        {
            if (string.IsNullOrEmpty(sfAccount))
            {
                return false;
            }

            return await DeleteUserSfLink(predicate, new string[] { sfAccount });
        }

        public async Task<bool> DeleteUserSfLink(Guid? ID, string[] sfAccount)
        {
            return await DeleteUserSfLink(u => u.GUID == ID, sfAccount);
        }

        public async Task<bool> DeleteUserSfLink(string login, string[] sfAccount)
        {
            return await DeleteUserSfLink(u => u.Login == login, sfAccount);
        }

        public async Task<bool> DeleteUserSfLink(Expression<Func<User, bool>> predicate, string[] sfAccount)
        {
            if (sfAccount == null)
            {
                return false;
            }

            try
            {
                var user = await _userRepo.GetByAsync(predicate);
                if (user == null)
                {
                    return false;
                }

                var platformsSet = new HashSet<string>(sfAccount);
                var sfLinks = _userSfLinkRepo.List(ua => ua.UserGUID == user.GUID && (platformsSet.Contains(ua.SalesforceAccountId)));
                if (sfLinks == null)
                {
                    return true;
                }

                if (!(await _userSfLinkRepo.DeleteRange(sfLinks)))
                {
                    return false;
                }
            }

            catch (Exception)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Delete all sf account links 

        public async Task<bool> DeleteUserAllSfLinks(Guid? ID)
        {
            return await DeleteUserAllAccesses(u => u.GUID == ID);
        }

        public async Task<bool> DeleteUserAllSfLinks(string login)
        {
            return await DeleteUserAllAccesses(u => u.Login == login);
        }

        public async Task<bool> DeleteUserAllSfLinks(Expression<Func<User, bool>> predicate)
        {
            try
            {
                var user = await _userRepo.GetByAsync(predicate);
                if (user == null)
                {
                    return false;
                }

                var userSfLinks = _userSfLinkRepo.List(ua => ua.UserGUID == user.GUID).ToList();
                if (userSfLinks == null || userSfLinks.Count == 0)
                {
                    return true;
                }

                if (!(await _userSfLinkRepo.DeleteRange(userSfLinks)))
                {
                    return false;
                }
            }

            catch (Exception)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Add sf account links

        public async Task<bool> AddUserSfLink(string login, string sfAccount)
        {
            return await AddUserSfLink(u => u.Login == login, sfAccount);
        }

        public async Task<bool> AddUserSfLink(Guid? ID, string sfAccount)
        {
            return await AddUserSfLink(u => u.GUID == ID, sfAccount);
        }

        public async Task<bool> AddUserSfLink(Expression<Func<User, bool>> predicate, string sfAccount)
        {
            if (string.IsNullOrEmpty(sfAccount))
            {
                return false;
            }

            return await AddUserSfLink(predicate, new string[] { sfAccount });
        }

        public async Task<bool> AddUserSfLink(string login, IEnumerable<string> sfAccount)
        {
            return await AddUserSfLink(u => u.Login == login, sfAccount);
        }

        public async Task<bool> AddUserSfLink(Guid? ID, IEnumerable<string> sfAccount)
        {
            return await AddUserSfLink(u => u.GUID == ID, sfAccount);
        }

        public async Task<bool> AddUserSfLink(Expression<Func<User, bool>> predicate, IEnumerable<string> sfAccount)
        {
            if (sfAccount == null)
            {
                return false;
            }

            try
            {
                var user = await _userRepo.GetByAsync(predicate);
                if (user == null)
                {
                    return false;
                }

                var sfAccountSet = new HashSet<string>(sfAccount);

                var existingsSfLinks = new HashSet<string>(await _userSfLinkRepo.List(u => u.UserGUID == user.GUID && sfAccountSet.Contains(u.SalesforceAccountId))
                                            .Select(u => u.SalesforceAccountId)
                                            .ToListAsync());

                var sfLinks = new List<UserSalesforceLink>();
                foreach(var acc in sfAccount)
                {
                    if (!existingsSfLinks.Contains(acc))
                    {
                        sfLinks.Add(new UserSalesforceLink
                        {
                            UserGUID = (Guid)user.GUID,
                            SalesforceAccountId = acc
                        });
                    }
                }

                if (!(await _userSfLinkRepo.AddRange(sfLinks)))
                {
                    return false;
                }
            }

            catch (Exception)
            {
                return false;
            }

            return true;
        }

        #endregion

        #endregion

        #region access CRUD

        #region delete access

        public async Task<bool> DeleteUserAccess(Guid? ID, string platform)
        {
            return await DeleteUserAccess(u => u.GUID == ID, platform);
        }

        public async Task<bool> DeleteUserAccess(string login, string platform)
        {
            return await DeleteUserAccess(u => u.Login == login, platform);
        }

        public async Task<bool> DeleteUserAccess(Expression<Func<User, bool>> predicate, string platform)
        {
            if (string.IsNullOrEmpty(platform))
            {
                return false;
            }

            return await DeleteUserAccess(predicate, new string[] { platform });
        }

        public async Task<bool> DeleteUserAccess(Guid? ID, string[] platforms)
        {
            return await DeleteUserAccess(u => u.GUID == ID, platforms);
        }

        public async Task<bool> DeleteUserAccess(string login, string[] platforms)
        {
           return await DeleteUserAccess(u => u.Login == login, platforms);
        }

        public async Task<bool> DeleteUserAccess(Expression<Func<User, bool>> predicate, string[] platforms)
        {
            if (platforms == null)
            {
                return false;
            }

            try
            {
                var user = await _userRepo.GetByAsync(predicate);
                if (user == null)
                {
                    return false;
                }

                var platformsSet = new HashSet<string>(platforms);
                var userAccesses = _userAccessRepo.List(ua => ua.UserGUID == user.GUID && (platformsSet.Contains(ua.PlatformFkNav.Name) || platformsSet.Contains(ua.PlatformFkNav.DomainName)));
                if (userAccesses == null)
                {
                    return true;
                }

                if (!(await _userAccessRepo.DeleteRange(userAccesses)))
                {
                    return false;
                }
            }

            catch (Exception)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region delete all accesses

        public async Task<bool> DeleteUserAllAccesses(Guid? ID)
        {
            return await DeleteUserAllAccesses(u => u.GUID == ID);
        }

        public async Task<bool> DeleteUserAllAccesses(string login)
        {
            return await DeleteUserAllAccesses(u => u.Login == login);
        }

        public async Task<bool> DeleteUserAllAccesses(Expression<Func<User, bool>> predicate)
        {
            try
            {
                var user = await _userRepo.GetByAsync(predicate);
                if (user == null)
                {
                    return false;
                }

                var userAccesses = _userAccessRepo.List(ua => ua.UserGUID == user.GUID).ToList();
                if (userAccesses == null || userAccesses.Count == 0)
                {
                    return true;
                }

                if ( !(await _userAccessRepo.DeleteRange(userAccesses)) )
                {
                    return false;
                }
            }

            catch (Exception)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region add access

        public async Task<bool> AddUserAccess(string login, string platform)
        {
            return await AddUserAccess(u => u.Login == login, platform);
        }

        public async Task<bool> AddUserAccess(Guid? ID, string platform)
        {
            return await AddUserAccess(u => u.GUID == ID, platform);
        }

        public async Task<bool> AddUserAccess(Expression<Func<User, bool>> predicate, string platform)
        {
            if (string.IsNullOrEmpty(platform))
            {
                return false;
            }

            return await AddUserAccess(predicate, new string[] { platform });
        }

        public async Task<bool> AddUserAccess(string login, IEnumerable<string> platforms)
        {
            return await AddUserAccess(u => u.Login == login, platforms);
        }

        public async Task<bool> AddUserAccess(Guid? ID, IEnumerable<string> platforms)
        {
            return await AddUserAccess(u => u.GUID == ID, platforms);
        }

        public async Task<bool> AddUserAccess(Expression<Func<User, bool>> predicate, IEnumerable<string> platforms)
        {
            if (platforms == null)
            {
                return false;
            }

            try
            {
                var user = await _userRepo.GetByAsync(predicate);
                if (user == null)
                {
                    return false;
                }

                var platformsSet = new HashSet<string>(platforms);

                var existingPlatforms = _platformRepo.List(p => platformsSet.Contains(p.Name) || platformsSet.Contains(p.DomainName));

                var accesses = new List<UserAccess>();
                foreach (var platform in existingPlatforms)
                {
                    accesses.Add(new UserAccess
                    {
                        UserGUID = user.GUID,
                        PlatformID = platform.ID
                    });
                }

                if (!(await _userAccessRepo.AddRange(accesses)))
                {
                    return false;
                }
            }

            catch (Exception)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region update coordinates

        public async Task<bool> UpdateUserCoordinates(string login, UserDTO newCoordinates)
        {
            return await UpdateUserCoordinates(u => u.Login == login, newCoordinates);
        }

        public async Task<bool> UpdateUserCoordinates(Guid guid, UserDTO newCoordinates)
        {
            return await UpdateUserCoordinates(u => u.GUID == guid, newCoordinates);
        }

        public async Task<bool> UpdateUserCoordinates(Expression<Func<User, bool>> predicates, UserDTO newCoordinates)
        {
            var userToUpdate = await _userRepo.GetByAsync(predicates);
            if (userToUpdate == null)
            {
                return false;
            }

            newCoordinates.Login ??= userToUpdate.Login;
            userToUpdate = _mapper.Map<UserDTO, User>(newCoordinates, userToUpdate);
            try
            {
                return await _userRepo.Update(userToUpdate);
            }

            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #endregion

        #region user enabler/disabler

        #region enable user

        public async Task<Tuple<int, IHttpMessage>> EnableUser(string login)
        {
            return await EnableUser(u => u.Login == login);
        }

        public async Task<Tuple<int, IHttpMessage>> EnableUser(Guid? ID)
        {
            return await EnableUser(u => u.GUID == ID);
        }

        public async Task<Tuple<int, IHttpMessage>> EnableUser(Expression<Func<User, bool>> predicate)
        {
            try
            {
                var user = await _userRepo.GetByAsync(predicate);
                if (user == null)
                {
                    return Tuple.Create(404, new HttpErrorMessage("Failed to retrieve user. Please ensure the token is valid.") as IHttpMessage);
                }

                if (user.IsActive)
                {
                    return Tuple.Create(400, new HttpErrorMessage("The user is already activated.") as IHttpMessage);
                }

                user.IsActive = true;
                if ( !(await _userRepo.Update(user)))
                {
                    return Tuple.Create(500, new HttpErrorMessage("Failed to enable the user.") as IHttpMessage);
                }

                return Tuple.Create(200, new HttpSuccessMessage("The user has been activated") as IHttpMessage);
            }

            catch (Exception)
            {
                return Tuple.Create(500, new HttpErrorMessage("Failed to enable the user.") as IHttpMessage);
            }
        }

        #endregion

        #region disable user

        public async Task<Tuple<int, IHttpMessage>> DisableUser(string login)
        {
            return await DisableUser(u => u.Login == login);
        }

        public async Task<Tuple<int, IHttpMessage>> DisableUser(Guid? ID)
        {
            return await DisableUser(u => u.GUID == ID);
        }

        public async Task<Tuple<int, IHttpMessage>> DisableUser(Expression<Func<User, bool>> predicate)
        {
            try
            {
                var user = await _userRepo.GetByAsync(predicate);
                if (user == null)
                {
                    return Tuple.Create(404, new HttpErrorMessage("Failed to retrieve user. Please ensure the token is valid.") as IHttpMessage);
                }

                user.IsActive = false;

                if ( !(await _userRepo.Update(user)))
                {
                    return Tuple.Create(500, new HttpErrorMessage("Failed to disable the user.") as IHttpMessage);
                }

                return Tuple.Create(200, new HttpSuccessMessage("The user has been disabled") as IHttpMessage);
            }

            catch (Exception)
            {
                return Tuple.Create(500, new HttpErrorMessage("Failed to disable the user.") as IHttpMessage);
            }
        }

        #endregion

        #endregion

        #region user getter

        public async Task<User> GetUser(Guid? ID)
        {
            return await GetUser(u => u.GUID == ID);
        }

        public async Task<User> GetUser(string login)
        {
            return await GetUser(u => u.Login == login);
        }

        public async Task<User> GetUser(Expression<Func<User, bool>> predicate)
        {
            try
            {
                return await _userRepo.List(predicate).AsNoTracking().FirstOrDefaultAsync();
            }

            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region User SF Accounts associated

        public async Task<IEnumerable<UserSalesforceLink>> GetUserSfAccounts(Guid ID)
        {
            return await GetUserSfAccounts(u => u.UserGUID == ID);
        }

        public async Task<IEnumerable<UserSalesforceLink>> GetUserSfAccounts(string login)
        {
            var user = await GetUser(login);

            return await GetUserSfAccounts(u => u.UserGUID == user.GUID);
        }

        public async Task<IEnumerable<UserSalesforceLink>> GetUserSfAccounts(Expression<Func<UserSalesforceLink, bool>> predicate)
        {
            try
            {
                return await _userSfLinkRepo.List(predicate).AsNoTracking().ToListAsync();
            }

            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region access checking

        public async Task<bool> HasAccess(Guid? ID, string platform, PlatformValueType pType = PlatformValueType.DOMAIN_NAME)
        {
            return await HasAccess(u => u.GUID == ID, platform, pType);
        }

        public async Task<bool> HasAccess(string login, string platform, PlatformValueType pType = PlatformValueType.DOMAIN_NAME)
        {
            return await HasAccess(u => u.Login == login, platform, pType);
        }

        public async Task<bool> HasAccess(Expression<Func<User, bool>> predicate, string platform, PlatformValueType pType = PlatformValueType.DOMAIN_NAME)
        {
            if (string.IsNullOrEmpty(platform))
            {
                return false;
            }

            try
            {
                var user = await _userRepo.GetByAsync(predicate);
                if (user == null)
                {
                    return false;
                }

                return (await _userAccessRepo.GetByAsync(
                    ua => ua.UserGUID == user.GUID && 
                    (
                        (ua.PlatformFkNav.Name == platform && pType == PlatformValueType.APP_NAME)
                        ||
                        (ua.PlatformFkNav.DomainName == platform && pType == PlatformValueType.DOMAIN_NAME)
                    )
               )) != null;
            }

            catch (Exception)
            {
                return false;
            }
        }

        #endregion


    }
}
