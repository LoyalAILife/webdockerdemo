using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WebDockerDemo.Dal;
using WebDockerDemo.Model.Dtos;
using WebDockerDemo.Web.Common;

namespace WebDockerDemo.Web
{
    public class Startup
    {
        // �� Asp.Net Core 3.0 env ʹ�� IWebHostEnvironment ������ IHostingEnvironment
        public Startup(IWebHostEnvironment env)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
        }

        public IConfiguration Configuration { get; }

        public ILifetimeScope AutofacContainer { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // ��Ҫ�����򷵻��κ�IServiceProvider������ConfigureContainer���������ᱻ���á�
        // �������ĳ��ԭ������Ҫ���ù�������������ô������ʹ�÷������չ����GetAutofacRoot��
        // this.AutofacContainer = app.ApplicationServices.GetAutofacRoot();

        public void ConfigureServices(IServiceCollection services)
        {
            AppSettings.Init(Configuration);

            // ��� Swagger ����
            services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Docker Demo",
                    Version = "v1"
                });

                // ���� xml ע���ĵ�
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                option.IncludeXmlComments(xmlPath);

                // ��� JWT ��֤
                option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "���¿�����������ͷ����Ҫ���Jwt ��ȨToken��Bearer Token",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });

                option.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme{ Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }}, new string[]{}
                    }
                });
            });

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = AppSettings.JwtSetting.Issuer,
                        ValidAudience = AppSettings.JwtSetting.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AppSettings.JwtSetting.SecurityKey)),
                        ClockSkew = TimeSpan.Zero   // Ĭ������ 300s ��ʱ��ƫ����������Ϊ 0
                    };
                });

            // ���ÿ���
            services.AddCors(options =>
            {
                options.AddPolicy("any", builder =>
                {
                    builder
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowAnyOrigin();
                });
            });

            // ���� EF
            services.AddDbContextPool<DemoDbContext>(option =>
            {
                option.UseMySql(Configuration.GetConnectionString("DemoContext"));
            });

            services.AddOptions();

            services.AddControllers();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule(new ApplicationModule());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseAuthentication();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "My Api v1"); });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            //CORS �м����������Ϊ�ڶ� UseRouting �� UseEndpoints�ĵ���֮��ִ�С� ���ò���ȷ�������м��ֹͣ�������С�
            app.UseCors("any");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}