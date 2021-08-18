using AutoMapper;
using Azure.Api.Controllers;
using Azure.Api.Data;
using Azure.Api.Data.Mapper;
using Azure.Api.Data.Models;
using Azure.Api.Repository;
using Azure.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.IO;

namespace Azure.Api.Test
{
    class PasswordAuthentication
    {
        private string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJHdWlkIjoiMTFlY2E1NzktNDBkNS00M2Q3LTg1ZDktMWU3ZjBhMTM2NjBjIiwiQWRtaW4iOiJGYWxzZSIsIm5iZiI6MTYyNDk3NzQ0NywiZXhwIjoxNjMyODYxNDQ3LCJpYXQiOjE2MjQ5Nzc0NDd9.wsRKL_zfFStTNk57R_3sBykII9JtylBB914WslrDGQI";

        private IPasswordAuthenticationService _passAuthService;
        private IUserService _userService;
        private IMailService _mailService;
        private IConfiguration _config;
        private IJwtTokenValidatorService _jwtvService;

        private IDocumentStoreRepository<UserPassword> _userPwdRepo;

        private IMapper _mapper;
        private IDocumentStoreRepository<User> _userRepo;
        private IDocumentStoreRepository<UserAccess> _userAccessRepo;
        private IDocumentStoreRepository<Platform> _platformRepo;
        private IDocumentStoreRepository<UserSalesforceLink> _userSfLinkRepo;
        private IDocumentStoreRepository<Email> _mailRepo { get; set; }

        private DocumentStoreContext _dbContext;

        private PasswordAuthenticationController _pwdControl { get; set; }

        [SetUp]
        public void Setup()
        {
            // config setup
            var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables();

            _config = configBuilder.Build();


            // mapper setup
            var mapperConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfile(new DocumentProfile());
                mc.AddProfile(new DocumentTypeProfile());
                mc.AddProfile(new UserProfile());
                mc.AddProfile(new EmailProfile());
            });

            _mapper = mapperConfig.CreateMapper();

            // dbcontext setup
            DbContextOptionsBuilder<DocumentStoreContext> options = new DbContextOptionsBuilder<DocumentStoreContext>();
            options.UseNpgsql(_config.GetConnectionString("DocumentStoreConnectionString"));

            _dbContext = new DocumentStoreContext(options.Options);

            // repositories

            _userRepo = new DocumentStoreRepository<User>(_dbContext);
            _userAccessRepo = new DocumentStoreRepository<UserAccess>(_dbContext);
            _userPwdRepo = new DocumentStoreRepository<UserPassword>(_dbContext);
            _platformRepo = new DocumentStoreRepository<Platform>(_dbContext);
            _mailRepo = new DocumentStoreRepository<Email>(_dbContext);
            _userSfLinkRepo = new DocumentStoreRepository<UserSalesforceLink>(_dbContext);

            // document service

            _userService = new UserService(_userRepo, _userAccessRepo, _platformRepo, _userSfLinkRepo, _mapper);
            _mailService = new MailService(_config, _mailRepo, _mapper);
            _passAuthService = new PasswordAuthenticationService(_userRepo, _userPwdRepo);
            _jwtvService = new JwtTokenValidatorService(_config);

            // controller

            _pwdControl = new PasswordAuthenticationController(_passAuthService, _userService, _mailService, _config, _jwtvService, _mapper);
        }

        #region Login

        [Test]
        public void Login_Known_User_And_Right_Accesses()
        {
            var login = "jayson.gal@hotmail.fr";
            var password = "Test1Test1&1234";
            var platform = "test.com";

            var payload = new JObject();
            payload.Add("Login", JToken.FromObject(login));
            payload.Add("Password", JToken.FromObject(password));
            payload.Add("AppDomainName", JToken.FromObject(platform));

            var task = _pwdControl.Login(payload);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(200, results.StatusCode);
        }

        [Test]
        public void Login_All_Null()
        {
            var task = _pwdControl.Login(null);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(400, results.StatusCode);
        }

        [Test]
        public void Login_Incorrect_Password()
        {
            var login = "jayson.gal@hotmail.fr";
            var password = "5meBL-3_OKL3";
            var platform = "test.com";

            var payload = new JObject();
            payload.Add("Login", JToken.FromObject(login));
            payload.Add("Password", JToken.FromObject(password));
            payload.Add("AppDomainName", JToken.FromObject(platform));

            var task = _pwdControl.Login(payload);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(403, results.StatusCode);
        }

        [Test]
        public void Login_No_Access_To_Domain()
        {
            var login = "jayson.gal@hotmail.fr";
            var password = "5meBL-3_OKL4";
            var platform = "test3.com";

            var payload = new JObject();
            payload.Add("Login", JToken.FromObject(login));
            payload.Add("Password", JToken.FromObject(password));
            payload.Add("AppDomainName", JToken.FromObject(platform));

            var task = _pwdControl.Login(payload);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(403, results.StatusCode);
        }

        #endregion

        #region Change Password

        [Test]
        public void Change_Password_Working()
        {
            var login = "jayson.gal@hotmail.fr";
            var newPassword = "Test1Test1&1234";
            var oldPassword = "Test2Test1&1234";

            var payload = new JObject();
            payload.Add("Login", JToken.FromObject(login));
            payload.Add("NewPassword", JToken.FromObject(newPassword));
            payload.Add("OldPassword", JToken.FromObject(oldPassword));

            var task = _pwdControl.ChangePassword(token, payload);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(200, results.StatusCode);
        }

        [Test]
        public void Change_Password_All_Null()
        {
            var task = _pwdControl.ChangePassword(token, null);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(400, results.StatusCode);
        }

        [Test]
        public void Change_Password_Incorrect_Old_Password()
        {
            var login = "jayson.gal@hotmail.fr";
            var oldPassword = "5meBL-3_OKL4";
            var newPassword = "Test1Test1&1234";

            var payload = new JObject();
            payload.Add("Login", JToken.FromObject(login));
            payload.Add("NewPassword", JToken.FromObject(newPassword));
            payload.Add("OldPassword", JToken.FromObject(oldPassword));

            var task = _pwdControl.ChangePassword(token, payload);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(403, results.StatusCode);
        }

        [Test]
        public void Change_Password_Too_Short()
        {
            var login = "jayson.gal@hotmail.fr";
            var newPassword = "Te5&";
            var oldPassword = "Test1Test1&1234";

            var payload = new JObject();
            payload.Add("Login", JToken.FromObject(login));
            payload.Add("NewPassword", JToken.FromObject(newPassword));
            payload.Add("OldPassword", JToken.FromObject(oldPassword));

            var task = _pwdControl.ChangePassword(token, payload);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(400, results.StatusCode);
        }

        [Test]
        public void Change_Password_No_Uppercase()
        {
            var login = "jayson.gal@hotmail.fr";
            var newPassword = "test1test1&1234";
            var oldPassword = "Test1Test1&1234";

            var payload = new JObject();
            payload.Add("Login", JToken.FromObject(login));
            payload.Add("NewPassword", JToken.FromObject(newPassword));
            payload.Add("OldPassword", JToken.FromObject(oldPassword));

            var task = _pwdControl.ChangePassword(token, payload);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(400, results.StatusCode);
        }

        [Test]
        public void Change_Password_No_LowerCase()
        {
            var login = "jayson.gal@hotmail.fr";
            var newPassword = "TEST1TEST1&1234";
            var oldPassword = "Test1Test1&1234";

            var payload = new JObject();
            payload.Add("Login", JToken.FromObject(login));
            payload.Add("NewPassword", JToken.FromObject(newPassword));
            payload.Add("OldPassword", JToken.FromObject(oldPassword));

            var task = _pwdControl.ChangePassword(token, payload);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(400, results.StatusCode);
        }

        [Test]
        public void Change_Password_No_Number()
        {
            var login = "jayson.gal@hotmail.fr";
            var newPassword = "Test&Test&Test";
            var oldPassword = "Test1Test1&1234";

            var payload = new JObject();
            payload.Add("Login", JToken.FromObject(login));
            payload.Add("NewPassword", JToken.FromObject(newPassword));
            payload.Add("OldPassword", JToken.FromObject(oldPassword));

            var task = _pwdControl.ChangePassword(token, payload);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(400, results.StatusCode);
        }

        [Test]
        public void Change_Password_No_Spe_Char()
        {
            var login = "jayson.gal@hotmail.fr";
            var newPassword = "Test1Test11234";
            var oldPassword = "Test1Test1&1234";

            var payload = new JObject();
            payload.Add("Login", JToken.FromObject(login));
            payload.Add("NewPassword", JToken.FromObject(newPassword));
            payload.Add("OldPassword", JToken.FromObject(oldPassword));

            var task = _pwdControl.ChangePassword(token, payload);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(400, results.StatusCode);
        }

        #endregion

        #region Reset Password

        [Test]
        public void Reset_Password()
        {
            string login = "jayson.gal@hotmail.fr";

            var payload = new JObject();
            payload.Add("Login", JToken.FromObject(login));

            var task = _pwdControl.ResetPassword(payload);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(200, results.StatusCode);
        }

        [Test]
        public void Reset_Password_Null()
        {
            string login = null;

            var payload = new JObject();
            payload.Add("Login", JToken.FromObject(login));

            var task = _pwdControl.ResetPassword(payload);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(400, results.StatusCode);
        }

        [Test]
        public void Reset_Password_Incorrect()
        {
            string login = "incorrect@lpcr.fr";

            var payload = new JObject();
            payload.Add("Login", JToken.FromObject(login));

            var task = _pwdControl.ResetPassword(payload);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(400, results.StatusCode);
        }

        #endregion
    }
}
