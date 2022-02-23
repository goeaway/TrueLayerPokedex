using System.Threading;
using System.Threading.Tasks;
using MediatR;
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
        private readonly ICacheWrapper<PokemonInfoDto> _cacheWrapper;
        private readonly IUtcNowProvider _nowProvider;
        private readonly IOptionsSnapshot<CachingOptions> _cachingOptions;

        public GetBasicPokemonInfoHandler(
            IPokemonService pokemonService, 
            ICacheWrapper<PokemonInfoDto> cacheWrapper, 
            IUtcNowProvider nowProvider, 
            IOptionsSnapshot<CachingOptions> cachingOptions)
        {
            _pokemonService = pokemonService;
            _cacheWrapper = cacheWrapper;
            _nowProvider = nowProvider;
            _cachingOptions = cachingOptions;
        }

        public async Task<OneOf<PokemonInfoDto, ErrorDto>> Handle(GetBasicPokemonInfoQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"basic:{request.PokemonName}";
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
            
            var result = new PokemonInfoDto
            {
                Name = pokemonResult.Data.Name,
                Description = pokemonResult.Data.Description,
                Habitat = pokemonResult.Data.Habitat,
                IsLegendary = pokemonResult.Data.IsLegendary
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