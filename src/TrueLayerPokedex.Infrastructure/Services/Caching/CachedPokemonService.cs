using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using TrueLayerPokedex.Application.Common;
using TrueLayerPokedex.Domain;
using TrueLayerPokedex.Domain.Models;
using TrueLayerPokedex.Domain.Options;

namespace TrueLayerPokedex.Infrastructure.Services.Caching
{
    public class CachedPokemonService : IPokemonService
    {
        // Using Scrutor nuget package to make this injection possible
        private readonly IPokemonService _pokemonService;
        private readonly IDistributedCache _distributedCache;
        private readonly IUtcNowProvider _nowProvider;
        private readonly CachingOptions _cachingOptions;
        
        public CachedPokemonService(
            IPokemonService pokemonService, 
            IDistributedCache distributedCache, 
            IUtcNowProvider nowProvider,
            IOptionsSnapshot<CachingOptions> cachingOptions)
        {
            _pokemonService = pokemonService;
            _distributedCache = distributedCache;
            _nowProvider = nowProvider;
            _cachingOptions = cachingOptions.Value;
        }

        public async Task<PokemonServiceResponse> GetPokemonDataAsync(string pokemonName, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(pokemonName, nameof(pokemonName));
            
            var cacheKey = $"basic:{pokemonName}";
            var cachedPokemonInfo = await _distributedCache.GetAsync(cacheKey, cancellationToken);
            if (cachedPokemonInfo?.Length > 0)
            {
                var data = JsonSerializer.Deserialize<PokemonInfo>(cachedPokemonInfo);

                return new PokemonServiceResponse
                {
                    Data = data,
                    Success = true,
                    StatusCode = HttpStatusCode.OK
                };
            }

            var result = await _pokemonService.GetPokemonDataAsync(pokemonName, cancellationToken);

            if (result.Success)
            {
                await _distributedCache.SetAsync(
                    cacheKey, 
                    Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result.Data)),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = _nowProvider.Now.Add(_cachingOptions.Ttl) 
                    },
                    cancellationToken);
            }
            
            return result;
        }
    }
}