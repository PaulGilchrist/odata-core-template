﻿using API.Classes;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using OdataCoreTemplate.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.IO;
using System.Reflection;

namespace ODataCoreTemplate {
    public class Startup
    {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            // To make this demo simpler, we can use a memory only database populated with mock data
            services.AddDbContext<ApiDbContext>(opt => opt.UseInMemoryDatabase("ApiDb"), ServiceLifetime.Singleton);

            //// For this demo we are using an in-memory database, but later we will connect to an actual database
            //// https://docs.microsoft.com/en-us/ef/core/get-started/aspnetcore/new-db
            //var connection = @"data source=localhost;initial catalog=ApiDev;integrated security=True;MultipleActiveResultSets=True;ConnectRetryCount=3";
            //services.AddDbContext<ApiDbContext>(options => options.UseSqlServer(connection, o => o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(3), null)));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
               .AddJwtBearer(options => {
                   options.Authority = "https://login.microsoftonline.com/" + Configuration.GetValue<string>("Security:TenantIdentifier");
                   options.TokenValidationParameters = new TokenValidationParameters {
                       ValidAudiences = Configuration.GetValue<string>("Security:AllowedAudiences").Split(',')
                   };
               });
            services.AddMvc(options => options.EnableEndpointRouting = false).SetCompatibilityVersion(CompatibilityVersion.Latest)
                .AddJsonOptions(options => {
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                });
            services.AddApiVersioning(options => options.ReportApiVersions = true);
            services.AddOData().EnableApiVersioning();
            services.AddODataApiExplorer(
                options => {
                    // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                    // note: the specified format code will format the version as "'v'major[.minor][-status]"
                    options.GroupNameFormat = "'v'VVV";
                    // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                    // can also be used to control the format of the API version in route templates
                    options.SubstituteApiVersionInUrl = true;
                    // configure query options (which cannot otherwise be configured by OData conventions)
                    //options.QueryOptions.Controller<UsersController>()
                    //    .Action(c => c.Get()).Allow(AllowedQueryOptions.All);
                    //options.QueryOptions.Controller<AddressesController>()
                    //    .Action(c => c.Get()).Allow(AllowedQueryOptions.All);
                });
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            services.AddHttpContextAccessor();
            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(options => {
                // add a custom operation filter which sets default values
                options.OperationFilter<SwaggerDefaultValues>();
                // integrate xml comments
                options.IncludeXmlComments(XmlCommentsFilePath);
            });
        }

        /// <summary>
        /// Configures the application using the provided builder, hosting environment, and logging factory.
        /// </summary>
        /// <param name="app">The current application builder.</param>
        /// <param name="env">The current hosting environment.</param>
        /// <param name="modelBuilder">The <see cref="VersionedODataModelBuilder">model builder</see> used to create OData entity data models (EDMs).</param>
        /// <param name="provider">The API version descriptor provider used to enumerate defined API versions.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IHttpContextAccessor httpContextAccessor, VersionedODataModelBuilder modelBuilder, IApiVersionDescriptionProvider provider) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseAuthentication();
            //Add mock data to the database if it is empty (demo uses in memory database only, so always starts empty)
            var context = app.ApplicationServices.GetService<ApiDbContext>();
            OdataCoreTemplate.Data.MockData.AddMockData(context);
            //Add custom telemetry initializer to add user name from the HTTP context
            var configuration = app.ApplicationServices.GetService<TelemetryConfiguration>();
            configuration.TelemetryInitializers.Add(new TelemetryInitializer(httpContextAccessor));
            app.UseMvc(routes => {
                // Swagger will not find controllers using conventional routing.  Attribute routing is required.
                // Also, OData controller base class opts out of the API Explorer
                //routes.MapODataServiceRoute("ODataRoute", "odata", GetEdmModel(app.ApplicationServices));
                routes.MapVersionedODataRoutes("ODataRoute", null, modelBuilder.GetEdmModels());
                routes.EnableDependencyInjection();
            });
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();
            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(options => {
                foreach (var description in provider.ApiVersionDescriptions) {
                    options.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        description.GroupName.ToUpperInvariant());
                }
                //options.SwaggerEndpoint("/swagger/v1/swagger.json", "OData Core Template API v1");
                options.DocExpansion(DocExpansion.None);
            });
        }

        static string XmlCommentsFilePath {
            get {
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var fileName = typeof(Startup).GetTypeInfo().Assembly.GetName().Name + ".xml";
                return Path.Combine(basePath, fileName);
            }
        }


    }
}
