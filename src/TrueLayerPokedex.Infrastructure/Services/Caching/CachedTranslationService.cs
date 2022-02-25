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
    public class CachedTranslationService : ITranslationService
    {
        // This injection is enabled by Scrutor
        private readonly ITranslationService _translationService;
        private readonly IDistributedCache _distributedCache;
        private readonly IUtcNowProvider _nowProvider;
        private readonly CachingOptions _cachingOptions;
        
        public CachedTranslationService(
            ITranslationService translationService, 
            IDistributedCache distributedCache, 
            IUtcNowProvider nowProvider,
            IOptionsSnapshot<CachingOptions> cachingOptions)
        {
            _translationService = translationService;
            _distributedCache = distributedCache;
            _nowProvider = nowProvider;
            _cachingOptions = cachingOptions.Value;
        }
        
        public async Task<PokemonInfo> GetTranslationAsync(PokemonInfo pokemonInfo, CancellationToken cancellationToken)
        {
            Guard.Against.Null(pokemonInfo, nameof(pokemonInfo));
            Guard.Against.NullOrWhiteSpace(pokemonInfo.Name, nameof(pokemonInfo.Name));
            
            var cacheKey = $"translated:{pokemonInfo.Name}";
            var cachedPokemonInfo = await _distributedCache.GetAsync(cacheKey, cancellationToken);
            if (cachedPokemonInfo?.Length > 0)
            {
                return JsonSerializer.Deserialize<PokemonInfo>(cachedPokemonInfo);
            }

            var result = await _translationService.GetTranslationAsync(pokemonInfo, cancellationToken);

            await _distributedCache.SetAsync(
                cacheKey, 
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result)),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = _nowProvider.Now.Add(_cachingOptions.Ttl) 
                },
                cancellationToken);
            
            return result;
        }
    }
}