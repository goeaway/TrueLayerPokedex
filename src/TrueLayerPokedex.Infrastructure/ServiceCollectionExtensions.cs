using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrueLayerPokedex.Application.Common;
using TrueLayerPokedex.Infrastructure.Services.Pokemon;
using TrueLayerPokedex.Infrastructure.Services.Translation;

namespace TrueLayerPokedex.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services,
            IConfiguration configuration)
        {
            return services
                .AddPokemonService(configuration)
                .AddTranslation(configuration);
        }

        private static IServiceCollection AddPokemonService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<IPokemonService, PokemonService>(client =>
            {
                client.BaseAddress = new Uri(configuration["PokemonApi:BaseUrl"]);
            });

            return services;
        }

        private static IServiceCollection AddTranslation(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<ITranslator, YodaTranslator>(client =>
            {
                client.BaseAddress = new Uri(configuration["Translations:BaseUrl"]);
            });
            
            services.AddHttpClient<ITranslator, ShakespeareTranslator>(client =>
            {
                client.BaseAddress = new Uri(configuration["Translations:BaseUrl"]);
            });
            
            services.AddTransient<ITranslationService, TranslationService>();

            return services;
        }
    }
}