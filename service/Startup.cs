using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using service.Models;
using service.Utils;

namespace service
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Config = new ConfigWrap(Configuration);
        }

        public IConfiguration Configuration { get; }
        public ConfigWrap Config { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddMvc(options => options.EnableEndpointRouting = false);
            services.AddMvcCore().AddApiExplorer();
            services.AddApiVersioning(option =>
            {
                option.ReportApiVersions = true;
                option.AssumeDefaultVersionWhenUnspecified = true;
                option.DefaultApiVersion = new ApiVersion(1, 0);
            });
                    // инициализируем движок MVC
            services.AddVersionedApiExplorer(o=> { o.GroupNameFormat = "'v'VVV"; o.SubstituteApiVersionInUrl = true; });
 
            // Регистрируем Swagger generator
            services.AddSwaggerGen(options => {
                var provider = services.BuildServiceProvider()
                    .GetRequiredService<IApiVersionDescriptionProvider>();
                var apiName = Assembly.GetExecutingAssembly().GetName().Name.Replace(".", " ");
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerDoc(
                        description.GroupName, 
                        new OpenApiInfo()
                        {
                            Title = $"{apiName} API",
                            Version = description.ApiVersion.ToString()
                        });
                }            
            });
            Task.Factory.StartNew(() =>
            {

            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseMvc();

            app.UseAuthorization();
                   // подключаем Swagger
            app.UseSwagger();
            // подключаем Swagger-UI
            app.UseSwaggerUI(options =>
            {
                var apiName = Assembly.GetExecutingAssembly().GetName().Name.Replace(".", " ");
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        $"{apiName} API {description.GroupName.ToUpperInvariant()}");
                }
            });

        }
    }
}


