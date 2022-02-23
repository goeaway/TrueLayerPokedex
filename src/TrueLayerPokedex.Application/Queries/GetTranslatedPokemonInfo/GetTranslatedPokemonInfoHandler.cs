using System.Text;
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
using TrueLayerPokedex.Domain.Options;

namespace TrueLayerPokedex.Application.Queries.GetTranslatedPokemonInfo
{
    public class GetTranslatedPokemonInfoHandler : IRequestHandler<GetTranslatedPokemonInfoQuery, OneOf<PokemonInfoDto, ErrorDto>>
    {
        private readonly IPokemonService _pokemonService;
        private readonly ITranslationService _translationService;
        private readonly ICacheWrapper<PokemonInfoDto> _cacheWrapper;
        private readonly IUtcNowProvider _nowProvider;
        private readonly IOptionsSnapshot<CachingOptions> _cachingOptions;

        public GetTranslatedPokemonInfoHandler(
            IPokemonService pokemonService, 
            ITranslationService translationService, 
            ICacheWrapper<PokemonInfoDto> cacheWrapper, 
            IUtcNowProvider nowProvider, 
            IOptionsSnapshot<CachingOptions> cachingOptions)
        {
            _pokemonService = pokemonService;
            _translationService = translationService;
            _cacheWrapper = cacheWrapper;
            _nowProvider = nowProvider;
            _cachingOptions = cachingOptions;
        }

        public async Task<OneOf<PokemonInfoDto, ErrorDto>> Handle(GetTranslatedPokemonInfoQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"translated:{request.PokemonName}";
            var cachedPokemonInfo = await _cacheWrapper.GetAsync(cacheKey, cancellationToken);
            if (cachedPokemonInfo != null)
            {
                return cachedPokemonInfo;
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
            
            await _cacheWrapper.SetAsync(
                cacheKey, 
                result,
                _nowProvider.Now.Add(_cachingOptions.Value.Ttl),
                cancellationToken);

            return result;
        }
    }
}