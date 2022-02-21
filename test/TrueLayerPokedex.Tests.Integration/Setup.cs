using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TrueLayerPokedex.Tests.Integration
{
    public static class Setup
    {
        public static (HttpClient, TestServer) CreateServer(Action<IServiceCollection> serviceConfigurer)
        {
            return CreateServer(serviceConfigurer, new Dictionary<string, string>());
        }
        
        public static (HttpClient, TestServer) CreateServer(
            Action<IServiceCollection> serviceConfigurer, 
            IDictionary<string, string> config)
        {
            var server = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((context, cfg) =>
                {
                    cfg.AddInMemoryCollection(config ?? new Dictionary<string, string>());
                })
                .ConfigureTestServices(serviceConfigurer)
            );

            var client = server.CreateClient();

            return (client, server);
        }
    }
}