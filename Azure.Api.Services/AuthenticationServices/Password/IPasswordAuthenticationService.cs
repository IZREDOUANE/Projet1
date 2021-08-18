using Azure.Api.Data.DTOs;
using Azure.Api.Data.Models;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Azure.Api.Services
{
    public interface IPasswordAuthenticationService
    {
        /// <summary>
        /// Allows the user to be authenticated to the API
        /// </summary>
        /// <param name="login">username of the user</param>
        /// <param name="password">password of the user</param>
        /// <returns></returns>
        Task<bool> Authenticate(string login, string password);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ID">GUID of the user from the DB</param>
        /// <param name="password">password of the user</param>
        /// <returns></returns>
        Task<bool> Authenticate(Guid? ID, string password);

        /// <summary>
        /// upsert a new password to the user (encrypted)
        /// </summary>
        /// <param name="login">username of the user</param>
        /// <param name="newPassword">new password of the user</param>
        /// <param name="oldPassword">old password of the user</param>
        /// <param name="verifyOld">do we have to verify the old password before upserting?</param>
        /// <returns>HTTP code + HTTP message</returns>
        Task<Tuple<int, IHttpMessage>> UpsertPassword(string login, string newPassword, string oldPassword = "", bool verifyOld = true);

        /// <summary>
        /// upsert a new password to the user (encrypted)
        /// </summary>
        /// <param name="ID">GUID of the user from the DB</param>
        /// <param name="newPassword">new password of the user</param>
        /// <param name="oldPassword">old password of the user</param>
        /// <param name="verifyOld">do we have to verify the old password before upserting?</param>
        /// <returns>HTTP code + HTTP message</returns>
        Task<Tuple<int, IHttpMessage>> UpsertPassword(Guid? ID, string newPassword, string oldPassword = "", bool verifyOld = true);

        /// <summary>
        /// upsert a new password to the user (encrypted)
        /// </summary>
        /// <param name="ID">GUID of the user from the DB</param>
        /// <param name="newPassword">new password of the user</param>
        /// <param name="oldPassword">old password of the user</param>
        /// <param name="verifyOld">do we have to verify the old password before upserting?</param>
        /// <returns>HTTP code + HTTP message</returns>
        Task<Tuple<int, IHttpMessage>> UpsertPassword(Expression<Func<User, bool>> predicate, string newPassword, string oldPassword = "", bool verifyOld = true);

        /// <summary>
        /// Generates a plain text password using LPCr criterias
        /// </summary>
        /// <returns></returns>
        string GeneratePlainPassword();

        /// <summary>
        /// Checks if the password given meets the LPCR criterias
        /// </summary>
        /// <param name="password">plain text password</param>
        /// <returns>a boolean that confirms if the password is valid</returns>
        bool MeetsCriterias(string password);
    }
}
