using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrueLayerPokedex.Application.Common;
using TrueLayerPokedex.Infrastructure.Services.Caching;
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

            // decoration allows us to "wrap" the normal pokemon service ^ with more functionality, caching
            services.Decorate<IPokemonService, CachedPokemonService>();

            return services;
        }

        private static IServiceCollection AddTranslation(this IServiceCollection services, IConfiguration configuration)
        {
            // The order of this registration is important.
            // If the shakespeare translator was added first, the yoda translator would never be used.
            // Adding the translators as HttpClients allows them to each get their own HttpClient injected.
            // Each get their own instance, but the underlying HttpMessageHandler is shared between them all.
            services.AddHttpClient<ITranslator, YodaTranslator>(client =>
            {
                client.BaseAddress = new Uri(configuration["Translations:BaseUrl"]);
            });
            
            services.AddHttpClient<ITranslator, ShakespeareTranslator>(client =>
            {
                client.BaseAddress = new Uri(configuration["Translations:BaseUrl"]);
            });
            
            services.AddTransient<ITranslationService, TranslationService>();

            // decoration allows us to "wrap" the normal translation service ^ with more functionality, caching
            services.Decorate<ITranslationService, CachedTranslationService>();

            return services;
        }
    }
}