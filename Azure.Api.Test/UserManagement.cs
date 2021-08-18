using AutoMapper;
using Azure.Api.Controllers;
using Azure.Api.Data;
using Azure.Api.Data.DTOs;
using Azure.Api.Data.Mapper;
using Azure.Api.Data.Models;
using Azure.Api.Repository;
using Azure.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.IO;

namespace Azure.Api.Test
{
    class UserManagement
    {
        private IUserService _userService;
        private IPasswordAuthenticationService _pwdAuthService;
        private IMailService _mailService;
        private IConfiguration _config;

        private IMapper _mapper;
        private IDocumentStoreRepository<User> _userRepo;
        private IDocumentStoreRepository<UserAccess> _userAccessRepo;
        private IDocumentStoreRepository<Platform> _platformRepo;
        private IDocumentStoreRepository<UserPassword> _userPwdRepo;
        private IDocumentStoreRepository<Email> _mailRepo;
        private IDocumentStoreRepository<UserSalesforceLink> _userSfLinkRepo;

        private DocumentStoreContext _dbContext;

        private UserManagementController _userControl;

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
            });

            _mapper = mapperConfig.CreateMapper();

            // dbcontext setup

            DbContextOptionsBuilder<DocumentStoreContext> options = new DbContextOptionsBuilder<DocumentStoreContext>();
            options.UseNpgsql(_config.GetConnectionString("DocumentStoreConnectionString"));

            _dbContext = new DocumentStoreContext(options.Options);

            // repositories

            _userRepo = new DocumentStoreRepository<User>(_dbContext);
            _userAccessRepo = new DocumentStoreRepository<UserAccess>(_dbContext);
            _platformRepo = new DocumentStoreRepository<Platform>(_dbContext);
            _userPwdRepo = new DocumentStoreRepository<UserPassword>(_dbContext);
            _mailRepo = new DocumentStoreRepository<Email>(_dbContext);
            _userSfLinkRepo = new DocumentStoreRepository<UserSalesforceLink>(_dbContext);

            // services

            _userService = new UserService(_userRepo, _userAccessRepo, _platformRepo, _userSfLinkRepo, _mapper);
            _pwdAuthService = new PasswordAuthenticationService(_userRepo, _userPwdRepo);
            _mailService = new MailService(_config, _mailRepo, _mapper);

            // controller

            _userControl = new UserManagementController(_userService, _pwdAuthService, _mailService, null, _config);
        }

        #region signup

        [Test]
        public void SignUp_Best_Case_Scenario() // Will not work since the url generation requires a web service to run
        {
            var user = new UserDTO
            {
                Firstname = "Jayson",
                LastName = "Galante",
                Login = "galante.jayson@gmail.com"
            };

            var authorizedPlatforms = new string[]
            {
                "test.com",
                "test2.com"
            };

            var obj = new JObject();
            obj.Add("Infos", JToken.FromObject(user));
            obj.Add("AuthorizedPlatforms", JToken.FromObject(authorizedPlatforms));

            var task = _userControl.SignUp(obj);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(200, results.StatusCode);
        }

        [Test]
        public void SignUp_Incorrect_Email() // Will not work since the url generation requires a web service to run
        {
            var user = new UserDTO
            {
                Firstname = "Jayson",
                LastName = "Galante",
                Login = "galante.jayson@gmail.co"
            };

            var authorizedPlatforms = new string[]
            {
                "test.com",
                "test2.com"
            };

            var obj = new JObject();
            obj.Add("Infos", JToken.FromObject(user));
            obj.Add("AuthorizedPlatforms", JToken.FromObject(authorizedPlatforms));

            var task = _userControl.SignUp(obj);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(200, results.StatusCode);
        }

        [Test]
        public void SignUp_No_Email() // Will not work since the url generation requires a web service to run
        {
            var user = new UserDTO
            {
                Firstname = "Jayson",
                LastName = "Galante"
            };

            var authorizedPlatforms = new string[]
            {
                "test.com",
                "test2.com"
            };

            var obj = new JObject();
            obj.Add("Infos", JToken.FromObject(user));
            obj.Add("AuthorizedPlatforms", JToken.FromObject(authorizedPlatforms));

            var task = _userControl.SignUp(obj);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(400, results.StatusCode);
        }

        [Test]
        public void SignUp_No_SfId() // Will not work since the url generation requires a web service to run
        {
            var user = new UserDTO
            {
                Firstname = "Jayson",
                LastName = "Galante",
                Login = "galante.jayson@gmail.co"
            };

            var authorizedPlatforms = new string[]
            {
                "test.com",
                "test2.com"
            };

            var obj = new JObject();
            obj.Add("Infos", JToken.FromObject(user));
            obj.Add("AuthorizedPlatforms", JToken.FromObject(authorizedPlatforms));

            var task = _userControl.SignUp(obj);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(400, results.StatusCode);
        }

        [Test]
        public void SignUp_No_authorizedPlatforms_In_Input() // Will not work since the url generation requires a web service to run
        {
            var user = new UserDTO
            {
                Firstname = "Jayson",
                LastName = "Galante",
                Login = "galante.jayson@gmail.co"
            };

            var authorizedPlatforms = new string[]
            {
                "test.com",
                "test2.com"
            };

            var obj = new JObject();
            obj.Add("Infos", JToken.FromObject(user));
            // obj.Add("AuthorizedPlatforms", JToken.FromObject(authorizedPlatforms));

            var task = _userControl.SignUp(obj);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(400, results.StatusCode);
        }

        [Test]
        public void SignUp_Empty_authorizedPlatforms() // Will not work since the url generation requires a web service to run
        {
            var user = new UserDTO
            {
                Firstname = "Jayson",
                LastName = "Galante",
                Login = "galante.jayson@gmail.co"
            };

            var authorizedPlatforms = new string[0];

            var obj = new JObject();
            obj.Add("Infos", JToken.FromObject(user));
            obj.Add("AuthorizedPlatforms", JToken.FromObject(authorizedPlatforms));

            var task = _userControl.SignUp(obj);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(200, results.StatusCode);
        }

        #endregion

        #region activate

        [Test]
        public void Activate_User_Account_Best_Case_Scenario()
        {
            var token = "3ceea95e-c8f1-4eca-ba40-3f2297408a6b";

            var task = _userControl.ActivateUser(Guid.Parse(token));
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(200, results.StatusCode);
        }

        [Test]
        public void Activate_User_Account_Already_Activated()
        {
            var token = "3ceea95e-c8f1-4eca-ba40-3f2297408a6b";

            var task = _userControl.ActivateUser(Guid.Parse(token));
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(400, results.StatusCode);
        }

        [Test]
        public void Activate_Unknown_GUID()
        {
            var token = "9e6fe51c-2e66-409e-84f7-a47ef274747a";

            var task = _userControl.ActivateUser(Guid.Parse(token));
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(404, results.StatusCode);
        }

        [Test]
        public void Activate_Null_GUID()
        {
            Guid? token = null;

            var task = _userControl.ActivateUser(token);
            task.Wait();

            var results = task.Result as ObjectResult;

            Assert.IsNotNull(results);
            Assert.AreEqual(400, results.StatusCode);
        }

        #endregion
    }
}
