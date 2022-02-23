using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Caching.Distributed;
using TrueLayerPokedex.Application.Common;

namespace TrueLayerPokedex.Infrastructure.Services.Caching
{
    // abstracts away the serialisation and deserialisation of the given type
    // we also cannot mock extension methods easily (which don't need to use the byte[] versions of methods),
    // which is why we don't use the DistributedCache directly in handlers
    public class CacheWrapper<T> : ICacheWrapper<T> where T : class
    {
        private readonly IDistributedCache _distributedCache;

        public CacheWrapper(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public async Task<T> GetAsync(string key, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(key, nameof(key));
            
            var cachedPokemonInfo = await _distributedCache.GetAsync(key, cancellationToken);
 
            if (cachedPokemonInfo != null && cachedPokemonInfo.Length > 0)
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(cachedPokemonInfo);
                }
                catch (JsonException)
                {
                    return null;
                }
            }

            return null;
        }

        public Task SetAsync(string key, T data, DateTime expiry, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(key, nameof(key));
            Guard.Against.Null(data, nameof(data));

            return _distributedCache.SetAsync(
                key, 
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data)),
                new DistributedCacheEntryOptions
                {   
                    AbsoluteExpiration = expiry
                },
                cancellationToken);
        }
    }
}