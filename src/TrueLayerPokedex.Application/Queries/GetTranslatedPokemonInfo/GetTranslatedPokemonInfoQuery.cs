using MediatR;
using TrueLayerPokedex.Domain.Dtos;
using OneOf;

namespace TrueLayerPokedex.Application.Queries.GetTranslatedPokemonInfo
{
    public class GetTranslatedPokemonInfoQuery : IRequest<OneOf<PokemonInfoDto, ErrorDto>>
    {
        public string PokemonName { get; set; }
    }
}