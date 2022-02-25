using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OneOf;
using TrueLayerPokedex.Application.Common;
using TrueLayerPokedex.Domain.Dtos;

namespace TrueLayerPokedex.Application.Queries.GetBasicPokemonInfo
{
    public class GetBasicPokemonInfoHandler : IRequestHandler<GetBasicPokemonInfoQuery, OneOf<PokemonInfoDto, ErrorDto>>
    {
        private readonly IPokemonService _pokemonService;

        public GetBasicPokemonInfoHandler(IPokemonService pokemonService)
        {
            _pokemonService = pokemonService;
        }

        public async Task<OneOf<PokemonInfoDto, ErrorDto>> Handle(GetBasicPokemonInfoQuery request, CancellationToken cancellationToken)
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
            
            return new PokemonInfoDto
            {
                Name = pokemonResult.Data.Name,
                Description = pokemonResult.Data.Description,
                Habitat = pokemonResult.Data.Habitat,
                IsLegendary = pokemonResult.Data.IsLegendary
            };
        }
    }
}