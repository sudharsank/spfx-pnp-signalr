﻿using System;
using System.Threading.Tasks;
using Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SignalRHub.Hubs;

namespace SignalRHub
{
    public class Startup
    {
        private readonly string Key = "2a596627-3c52-4851-b6f1-57301af61e3a";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
           
            services.AddCors();
            services.AddSignalR();

            Settings.StorageConnection = Configuration["AzureWebJobsDashboard"];
            var SecurityKey = new SymmetricSecurityKey(new Guid(Key).ToByteArray());

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            LifetimeValidator = (before, expires, token, parameters) => expires > DateTime.UtcNow,
                            ValidateAudience = false,
                            ValidateIssuer = false,
                            ValidateActor = false,
                            ValidateLifetime = true,
                            IssuerSigningKey = SecurityKey
                        };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];

                            if (!string.IsNullOrEmpty(accessToken) &&
                                (context.HttpContext.WebSockets.IsWebSocketRequest || context.Request.Headers["Accept"] == "text/event-stream"))
                            {
                                context.Token = context.Request.Query["access_token"];
                            }


                            return Task.CompletedTask;
                        }
                    };
                });

        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseAuthentication();

            app.UseCors(builder =>
            {
                builder.WithOrigins("https://localhost:4321", "https://mastaq.sharepoint.com")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });

            app.UseHttpsRedirection();

            app.UseSignalR(routes =>
            {
                routes.MapHub<PnPProvisioningHub>("/" + Consts.HubName);
            });
        }
    }
}