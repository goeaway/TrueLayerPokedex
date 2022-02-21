using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using OneOf;
using TrueLayerPokedex.Application.Common;
using TrueLayerPokedex.Domain;
using TrueLayerPokedex.Domain.Dtos;
using TrueLayerPokedex.Domain.Models;
using TrueLayerPokedex.Domain.Options;

namespace TrueLayerPokedex.Application.Behaviours
{
    public class CachingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> 
        where TRequest : ICacheableRequest<TResponse> 
        where TResponse : class
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IUtcNowProvider _nowProvider;
        private readonly CachingOptions _cachingOptions;

        public CachingBehaviour(IDistributedCache distributedCache, IUtcNowProvider nowProvider, IOptionsSnapshot<CachingOptions> cachingOptionsSnapshot)
        {
            _distributedCache = distributedCache;
            _nowProvider = nowProvider;
            _cachingOptions = cachingOptionsSnapshot.Value;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            var cachedResponse = await _distributedCache.GetAsync(request.CacheKey, cancellationToken);
            if (cachedResponse != null && cachedResponse.Length > 0)
            {
                var responseType = typeof(TResponse);
                if (responseType.Name == typeof(ResponseOrError<object>).Name)
                {
                    var t0Type = typeof(TResponse).GetGenericArguments().First();
                    var cacheResult = JsonSerializer.Deserialize(cachedResponse, t0Type);
                    return Activator.CreateInstance(responseType, cacheResult) as TResponse;
                }

                return JsonSerializer.Deserialize<TResponse>(cachedResponse);
            }

            var result = await next();
            
            await _distributedCache.SetAsync(
                request.CacheKey, 
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