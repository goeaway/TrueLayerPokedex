using System;
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
using TrueLayerPokedex.Domain.Options;

namespace TrueLayerPokedex.Application.Queries.GetBasicPokemonInfo
{
    public class GetBasicPokemonInfoHandler : IRequestHandler<GetBasicPokemonInfoQuery, OneOf<PokemonInfoDto, ErrorDto>>
    {
        private readonly IPokemonService _pokemonService;
        private readonly IDistributedCache _distributedCache;
        private readonly IUtcNowProvider _nowProvider;
        private readonly IOptionsSnapshot<CachingOptions> _cachingOptions;

        public GetBasicPokemonInfoHandler(
            IPokemonService pokemonService, 
            IDistributedCache distributedCache, 
            IUtcNowProvider nowProvider, 
            IOptionsSnapshot<CachingOptions> cachingOptions)
        {
            _pokemonService = pokemonService;
            _distributedCache = distributedCache;
            _nowProvider = nowProvider;
            _cachingOptions = cachingOptions;
        }

        public async Task<OneOf<PokemonInfoDto, ErrorDto>> Handle(GetBasicPokemonInfoQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"basic:{request.PokemonName}";
            var cachedPokemonInfo = await _distributedCache.GetAsync(cacheKey, cancellationToken);
            if (cachedPokemonInfo != null && cachedPokemonInfo.Length > 0)
            {
                return JsonSerializer.Deserialize<PokemonInfoDto>(cachedPokemonInfo);
            }
            
            var pokemonResult = await _pokemonService.GetPokemonDataAsync(request.PokemonName, cancellationToken);

            if (!pokemonResult.Success)
            {
                return new ErrorDto
                {
                    StatusCode = pokemonResult.StatusCode,
                    Message = pokemonResult.Message
                };
            }
            
            var result = new PokemonInfoDto
            {
                Name = pokemonResult.Data.Name,
                Description = pokemonResult.Data.Description,
                Habitat = pokemonResult.Data.Habitat,
                IsLegendary = pokemonResult.Data.IsLegendary
            };

            await _distributedCache.SetAsync(
                cacheKey, 
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result)),
                new DistributedCacheEntryOptions
                {   
                    AbsoluteExpiration = _nowProvider.Now.Add(_cachingOptions.Value.Ttl)
                },
                cancellationToken);

            return result;
        }
    }
}