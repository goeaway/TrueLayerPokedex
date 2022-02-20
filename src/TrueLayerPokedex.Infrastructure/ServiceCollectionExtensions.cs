using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrueLayerPokedex.Application.Common;
using TrueLayerPokedex.Infrastructure.Services.Pokemon;

namespace TrueLayerPokedex.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services,
            IConfiguration configuration)
        {
            return services
                .AddPokemonService(configuration);
        }

        private static IServiceCollection AddPokemonService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<IPokemonService, PokemonService>(client =>
            {
                client.BaseAddress = new Uri(configuration["PokemonApi:BaseUrl"]);
            });

            return services;
        }
    }
}