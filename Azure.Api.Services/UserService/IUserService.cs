using Azure.Api.Data.DTOs;
using Azure.Api.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Azure.Api.Services
{
    public interface IUserService
    {
        Task<User> UpsertUser<T>(T user, string[] sfAccounts = null, string[] platforms = null);

        Task<bool> DeleteUserSfLink(Guid? ID, string sfAccount);
        Task<bool> DeleteUserSfLink(string login, string sfAccount);
        Task<bool> DeleteUserSfLink(Expression<Func<User, bool>> predicate, string sfAccount);
        Task<bool> DeleteUserSfLink(Guid? ID, string[] sfAccount);
        Task<bool> DeleteUserSfLink(string login, string[] sfAccount);
        Task<bool> DeleteUserSfLink(Expression<Func<User, bool>> predicate, string[] sfAccount);

        Task<bool> DeleteUserAllSfLinks(Guid? ID);
        Task<bool> DeleteUserAllSfLinks(string login);
        Task<bool> DeleteUserAllSfLinks(Expression<Func<User, bool>> predicate);
        Task<bool> AddUserSfLink(Expression<Func<User, bool>> predicate, IEnumerable<string> sfAccount);

        Task<bool> AddUserSfLink(string login, string sfAccount);
        Task<bool> AddUserSfLink(Guid? ID, string sfAccount);
        Task<bool> AddUserSfLink(Expression<Func<User, bool>> predicate, string sfAccount);
        Task<bool> AddUserSfLink(string login, IEnumerable<string> sfAccount);
        Task<bool> AddUserSfLink(Guid? ID, IEnumerable<string> sfAccount);

        Task<IEnumerable<UserSalesforceLink>> GetUserSfAccounts(Guid ID);
        Task<IEnumerable<UserSalesforceLink>> GetUserSfAccounts(string login);
        Task<IEnumerable<UserSalesforceLink>> GetUserSfAccounts(Expression<Func<UserSalesforceLink, bool>> predicate);

        Task<bool> DeleteUserAccess(Guid? ID, string platform);
        Task<bool> DeleteUserAccess(string login, string platform);
        Task<bool> DeleteUserAccess(Expression<Func<User, bool>> predicate, string platform);
        Task<bool> DeleteUserAccess(Guid? ID, string[] platforms);
        Task<bool> DeleteUserAccess(string login, string[] platforms);
        Task<bool> DeleteUserAccess(Expression<Func<User, bool>> predicate, string[] platforms);

        Task<bool> DeleteUserAllAccesses(Guid? ID);
        Task<bool> DeleteUserAllAccesses(string login);
        Task<bool> DeleteUserAllAccesses(Expression<Func<User, bool>> predicate);

        Task<bool> AddUserAccess(string login, string platform);
        Task<bool> AddUserAccess(Guid? ID, string platform);
        Task<bool> AddUserAccess(Expression<Func<User, bool>> predicate, string platform);
        Task<bool> AddUserAccess(string login, IEnumerable<string> platforms);
        Task<bool> AddUserAccess(Guid? ID, IEnumerable<string> platforms);
        Task<bool> AddUserAccess(Expression<Func<User, bool>> predicate, IEnumerable<string> platforms);

        Task<Tuple<int, IHttpMessage>> EnableUser(string login);
        Task<Tuple<int, IHttpMessage>> EnableUser(Guid? ID);
        Task<Tuple<int, IHttpMessage>> EnableUser(Expression<Func<User, bool>> predicate);

        Task<Tuple<int, IHttpMessage>> DisableUser(string login);
        Task<Tuple<int, IHttpMessage>> DisableUser(Guid? ID);
        Task<Tuple<int, IHttpMessage>> DisableUser(Expression<Func<User, bool>> predicate);

        Task<User> GetUser(Guid? ID);
        Task<User> GetUser(string login);
        Task<User> GetUser(Expression<Func<User, bool>> predicate);

        Task<bool> UpdateUserCoordinates(string login, UserDTO newCoordinates);
        Task<bool> UpdateUserCoordinates(Guid guid, UserDTO newCoordinates);
        Task<bool> UpdateUserCoordinates(Expression<Func<User, bool>> predicates, UserDTO newCoordinates);

        Task<bool> HasAccess(Guid? ID, string platform, PlatformValueType pType = PlatformValueType.DOMAIN_NAME);
        Task<bool> HasAccess(string login, string platform, PlatformValueType pType = PlatformValueType.DOMAIN_NAME);
        Task<bool> HasAccess(Expression<Func<User, bool>> predicate, string platform, PlatformValueType pType = PlatformValueType.DOMAIN_NAME);
    }
}
