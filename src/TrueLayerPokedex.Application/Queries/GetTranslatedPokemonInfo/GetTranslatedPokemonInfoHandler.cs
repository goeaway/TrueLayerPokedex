﻿using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using TrueLayerPokedex.Domain.Dtos;
using OneOf;
using TrueLayerPokedex.Application.Common;
using TrueLayerPokedex.Domain;
using TrueLayerPokedex.Domain.Models;
using TrueLayerPokedex.Domain.Options;

namespace TrueLayerPokedex.Application.Queries.GetTranslatedPokemonInfo
{
    public class GetTranslatedPokemonInfoHandler : IRequestHandler<GetTranslatedPokemonInfoQuery, ResponseOrError<PokemonInfoDto>>
    {
        private readonly IPokemonService _pokemonService;
        private readonly ITranslationService _translationService;
        private readonly IDistributedCache _distributedCache;
        private readonly IUtcNowProvider _nowProvider;
        private readonly IOptionsSnapshot<CachingOptions> _cachingOptions;

        public GetTranslatedPokemonInfoHandler(
            IPokemonService pokemonService, 
            ITranslationService translationService, 
            IDistributedCache distributedCache, 
            IUtcNowProvider nowProvider, 
            IOptionsSnapshot<CachingOptions> cachingOptions)
        {
            _pokemonService = pokemonService;
            _translationService = translationService;
            _distributedCache = distributedCache;
            _nowProvider = nowProvider;
            _cachingOptions = cachingOptions;
        }

        public async Task<ResponseOrError<PokemonInfoDto>> Handle(GetTranslatedPokemonInfoQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"translated:{request.PokemonName}";
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

            var translatedResult = await _translationService.GetTranslationAsync(pokemonResult.Data, cancellationToken);
            
            var result = new PokemonInfoDto
            {
                Name = translatedResult.Name,
                Description = translatedResult.Description,
                Habitat = translatedResult.Habitat,
                IsLegendary = translatedResult.IsLegendary
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