using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Azure.Api.Data;
using Azure.Api.Services;
using Azure.Api.Services.Entreprise;
using Azure.Api.Repository;
using Microsoft.OpenApi.Models;
using Azure.Api.Services.Monitoring;
using Azure.Api.Repository.Monitoring;
using Microsoft.AspNetCore.HttpOverrides;
using AutoMapper;
using Azure.Api.Data.Mapper;
using System.IO;
using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Azure.Api
{
    public class Startup
    {
        public Startup()
        {
            var env = Environment.GetEnvironmentVariable("ENVIRONMENT");

            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{ env }.json", false)
                .AddEnvironmentVariables()
                .Build();
        }

        public IConfiguration Configuration { get; }

     
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConfiguration>(Configuration);

            services.AddCors(options =>
            {
                options.AddPolicy(name: "AllowAnyOrigins",
                                  builder =>
                                  {
                                      builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                                  });

                options.AddPolicy(name: "SSOAccessPolicy",
                                  builder =>
                                  {
                                      builder.WithOrigins(Configuration.GetSection("AllowOriginsSSO").Get<string[]>())
                                        .AllowAnyHeader()
                                        .AllowAnyMethod()
                                        .AllowCredentials();
                                  });
            });


            services.AddControllers()
                .AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

            services.AddDbContext<DocumentStoreContext>(options => options.UseNpgsql(Configuration.GetConnectionString("DocumentStoreConnectionString")));
            services.AddScoped(typeof(IAccountFamilleService), typeof(AccountFamilleService));
            services.AddScoped(typeof(IAccountEntrepriseService), typeof(AccountEntrepriseService));
            services.AddScoped(typeof(IUserService), typeof(UserService));
            services.AddScoped(typeof(IMailService), typeof(MailService));
            services.AddScoped(typeof(IDocumentStoreRepository<>), typeof(DocumentStoreRepository<>));
            services.AddSingleton(typeof(ISalesForceService), typeof(SalesForceService));
            services.AddTransient(typeof(IDocumentService), typeof(DocumentService));
            services.AddScoped(typeof(IAccountCrecheService), typeof(AccountCrecheService));
            services.AddTransient(typeof(IPasswordAuthenticationService), typeof(PasswordAuthenticationService));
            services.AddTransient(typeof(ISSOAuthenticationService), typeof(SSOAuthenticationService));
            services.AddScoped(typeof(IAdminService), typeof(AdminService));
            services.AddSingleton(typeof(IJwtTokenValidatorService), typeof(JwtTokenValidatorService));
            services.AddScoped(typeof(IMonitoringService), typeof(MonitoringService));
            services.AddScoped(typeof(IMonitoringRepository), typeof(MonitoringRepository));
            services.AddScoped(typeof(IIntranetViewerService), typeof(IntranetViewerService));

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Swagger", Version = "v1" });
            });

            string[] initialScopes = Configuration.GetValue<string>("DownstreamApi:Scopes")?.Split(' ');
            services
                    .AddAuthentication()
                    .AddJwtBearer(options =>
                    {
                        var tokenKey = Configuration.GetSection("APISecurity").GetValue<string>("TokenKey");

                        var key = Encoding.ASCII.GetBytes(tokenKey);
                        SecurityKey signingKey = new SymmetricSecurityKey(key);

                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            ValidateIssuerSigningKey = true,
                            ValidateLifetime = true,
                            IssuerSigningKey = signingKey
                        };
                    })
                    .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"))
                    .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
                    .AddMicrosoftGraph(Configuration.GetSection("DownstreamApi"))
                    .AddInMemoryTokenCaches();

            SetupAutoMapping(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            if (env.IsProduction() || env.IsStaging())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
            });
        }

        private void SetupAutoMapping(IServiceCollection services)
        {
            var mapperConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfile(new DocumentProfile());
                mc.AddProfile(new DocumentTypeProfile());
                mc.AddProfile(new UserProfile());
                mc.AddProfile(new EmailProfile());
            });

            var mapper = mapperConfig.CreateMapper();

            services.AddSingleton(mapper);
        }
    }
}
