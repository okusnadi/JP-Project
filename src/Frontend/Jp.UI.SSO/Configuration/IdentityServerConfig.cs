﻿using System;
using System.Reflection;
using IdentityServer4;
using Jp.Infra.CrossCutting.Identity.Entities.Identity;
using Jp.UI.SSO.Util;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace Jp.UI.SSO.Configuration
{
    public static class IdentityServerConfig
    {
        public static IServiceCollection AddIdentityServer(this IServiceCollection services,
            IConfiguration configuration, IHostingEnvironment environment, ILogger logger)
        {
            var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION") ?? configuration.GetConnectionString("SSOConnection");
            var issuerUri = Environment.GetEnvironmentVariable("ISSUER_URI") ?? "https://localhost:5000";
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;


            logger.LogInformation($"Authority URI: {issuerUri}");
            


            var builder = services.AddIdentityServer(
                    options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;
                    options.IssuerUri = issuerUri;
                    
                })
                .AddAspNetIdentity<UserIdentity>()
                // this adds the config data from DB (clients, resources)
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = b =>
                        b.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
                })
                // this adds the operational data from DB (codes, tokens, consents)
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = b =>
                        b.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));

                    // this enables automatic token cleanup. this is optional.
                    //options.EnableTokenCleanup = true;
                    //options.TokenCleanupInterval = 15; // frequency in seconds to cleanup stale grants. 15 is useful during debugging
                });

            builder.AddSigninCredentialFromConfig(configuration.GetSection("CertificateOptions"), logger, environment);
            //if (environment.IsDevelopment())
            //{
            //    builder.AddDeveloperSigningCredential(false);
            //}
            //else
            //{
            //    builder.AddSigninCredentialFromConfig(configuration.GetSection("CertificateOptions"), logger, environment);
            //}

            return services;
        }

    }
}