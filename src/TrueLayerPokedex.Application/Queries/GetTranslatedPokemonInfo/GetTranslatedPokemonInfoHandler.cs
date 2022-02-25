using System.Threading;
using System.Threading.Tasks;
using MediatR;
using TrueLayerPokedex.Domain.Dtos;
using OneOf;
using TrueLayerPokedex.Application.Common;

namespace TrueLayerPokedex.Application.Queries.GetTranslatedPokemonInfo
{
    public class GetTranslatedPokemonInfoHandler : IRequestHandler<GetTranslatedPokemonInfoQuery, OneOf<PokemonInfoDto, ErrorDto>>
    {
        private readonly IPokemonService _pokemonService;
        private readonly ITranslationService _translationService;

        public GetTranslatedPokemonInfoHandler(
            IPokemonService pokemonService, 
            ITranslationService translationService)
        {
            _pokemonService = pokemonService;
            _translationService = translationService;
        }

        public async Task<OneOf<PokemonInfoDto, ErrorDto>> Handle(GetTranslatedPokemonInfoQuery request, CancellationToken cancellationToken)
        {
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
            
            return new PokemonInfoDto
            {
                Name = translatedResult.Name,
                Description = translatedResult.Description,
                Habitat = translatedResult.Habitat,
                IsLegendary = translatedResult.IsLegendary
            };
        }
    }
}