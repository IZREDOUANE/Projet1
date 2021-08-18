using Azure.Api.Data.DTOs;
using Azure.Api.Data.Models;
using Azure.Api.Repository;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Azure.Api.Services
{
    public class PasswordAuthenticationService : IPasswordAuthenticationService
    {
        private readonly IDocumentStoreRepository<User> _userRepo;
        private readonly IDocumentStoreRepository<UserPassword> _userPwdRepo;

        private const int SALT_SIZE = 24;
        private const int HASH_SIZE = 24;
        private const int ITERATIONS = 100000;
        private const int PLAIN_PASSWORD_SIZE = 12;

        private readonly string[] ACCEPTABLE_PASSWORD_CHARS = new string[]
                                                                {
                                                                "abcdefghijklmnopqrstuvwxyz",
                                                                "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                                                                "0123456789",
                                                                "{([#:^,.?!|&_`~@$%/=+-*';])}"
                                                                };

        public PasswordAuthenticationService
        (
            IDocumentStoreRepository<User> userRepo,
            IDocumentStoreRepository<UserPassword> userPwdRepo
        )
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _userPwdRepo = userPwdRepo ?? throw new ArgumentNullException(nameof(userPwdRepo));
        }

        public async Task<bool> Authenticate(string login, string password)
        {
            var user = await _userRepo.GetByAsync(u => u.Login == login && u.IsActive);
            if (user == null)
            {
                return false;
            }

            return await AuthenticateProtocol(user.GUID, password);
        }

        public async Task<bool> Authenticate(Guid? ID, string password)
        {
            if (ID == null)
            {
                return false;
            }

            var user = await _userRepo.GetByAsync(u => u.GUID == ID);
            if (user == null)
            {
                return false;
            }

            return await AuthenticateProtocol(ID, password);
        }

        private async Task<bool> AuthenticateProtocol(Guid? ID, string password)
        {
            if (ID == null)
            {
                return false;
            }

            try
            {
                var refHashedPassword = await _userPwdRepo.GetByAsync(p => p.UserGUID == ID);
                if (refHashedPassword == null)
                {
                    return false;
                }

                var hashedAttemptedPassword = HashPassword(password, refHashedPassword.SaltKey);

                return hashedAttemptedPassword.Item2 == refHashedPassword.HashedPassword;
            }

            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to process password", e);
            }
        }

        public async Task<Tuple<int, IHttpMessage>> UpsertPassword(string login, string newPassword, string oldPassword = "", bool verifyOld = true)
        {
            return await UpsertPassword(u => u.Login == login, newPassword, oldPassword, verifyOld);
        }

        public async Task<Tuple<int, IHttpMessage>> UpsertPassword(Guid? ID, string newPassword, string oldPassword = "", bool verifyOld = true)
        {
            return await UpsertPassword(u => u.GUID == ID, newPassword, oldPassword, verifyOld);
        }

        public async Task<Tuple<int, IHttpMessage>> UpsertPassword(Expression<Func<User, bool>> predicate, string newPassword, string oldPassword="", bool verifyOld = true)
        {
            try
            {
                if (string.IsNullOrEmpty(newPassword))
                {
                    return Tuple.Create(400, new HttpErrorMessage("New password is empty") as IHttpMessage);
                }


                var user = await _userRepo.GetByAsync(predicate);
                if (user == null)
                {
                    return Tuple.Create(403, new HttpErrorMessage($"User and/or password are incorrect") as IHttpMessage);
                }

                var userPwd = await _userPwdRepo.GetByAsync(p => p.UserGUID == user.GUID);
                if (userPwd == null)
                {
                    userPwd = new UserPassword
                    {
                        UserGUID = user.GUID
                    };
                }

                else
                {
                    var loginSuccess = await AuthenticateProtocol(user.GUID, oldPassword);
                    if (verifyOld && !loginSuccess)
                    {
                        return Tuple.Create(403, new HttpErrorMessage($"user and/or password are incorrect") as IHttpMessage);
                    }
                }

                var newHashedPassword = HashPassword(newPassword);
                userPwd.SaltKey = newHashedPassword.Item1;
                userPwd.HashedPassword = newHashedPassword.Item2;

                if (userPwd.ID == 0)
                {
                    if ((await _userPwdRepo.Add(userPwd)) == null)
                    {
                        return Tuple.Create(500, new HttpErrorMessage("Server error: failed to change password") as IHttpMessage);
                    }
                }

                else
                {
                    if (!await _userPwdRepo.Update(userPwd))
                    {
                        return Tuple.Create(500, new HttpErrorMessage("Server error: failed to change password") as IHttpMessage);
                    }
                }

                return Tuple.Create(200, new HttpSuccessMessage("Password changed successfully") as IHttpMessage);
            }

            catch (Exception)
            {
                return Tuple.Create(500, new HttpErrorMessage("Server error: failed to change password") as IHttpMessage);
            }
        }

        private Tuple<string, string> HashPassword(string password, string salt=null)
        {
            byte[] saltBytes = new byte[SALT_SIZE];
            string hashedPassword;

            using (var provider = new RNGCryptoServiceProvider())
            {
                if (string.IsNullOrEmpty(salt))
                {
                    provider.GetBytes(saltBytes);   
                }

                else
                {
                    saltBytes = Convert.FromBase64String(salt);
                }

                salt = Convert.ToBase64String(saltBytes);

                using (var pbkfd2 = new Rfc2898DeriveBytes(password, saltBytes, ITERATIONS))
                using (var sha256Hasher = SHA256.Create())
                {
                    var hashedPbkfd2Password = pbkfd2.GetBytes(HASH_SIZE);
                    var hashedSHA256 = sha256Hasher.ComputeHash(hashedPbkfd2Password);

                    hashedPassword = Convert.ToBase64String(hashedSHA256);
                }
            }

            return Tuple.Create
            (
                salt,
                hashedPassword
            );
        }

        public bool MeetsCriterias(string password)
        {
            if (password.Length < PLAIN_PASSWORD_SIZE)
            {
                return false;
            }

            foreach (var criteria in ACCEPTABLE_PASSWORD_CHARS)
            {
                var hash = new HashSet<char>();
                foreach (var c in criteria)
                {
                    hash.Add(c);
                }

                var success = false;
                foreach (var c in password)
                {
                    if (hash.Contains(c))
                    {
                        success = true;
                        break;
                    }
                }

                if (!success)
                {
                    return false;
                }
            }

            return true;
        }

        public string GeneratePlainPassword()
        {
            var nbCharTypes = ACCEPTABLE_PASSWORD_CHARS.Length;

            var password = new StringBuilder();
            var rand = new Random();
            while (password.Length != PLAIN_PASSWORD_SIZE)
            {
                var charType = rand.Next(nbCharTypes);
                var selection = ACCEPTABLE_PASSWORD_CHARS[charType];

                password.Append(selection[rand.Next(selection.Length)]);
            }

            return password.ToString();
        }
    }
}
