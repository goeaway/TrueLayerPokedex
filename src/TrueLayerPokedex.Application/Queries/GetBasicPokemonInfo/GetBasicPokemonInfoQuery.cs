using MediatR;
using TrueLayerPokedex.Domain.Dtos;
using OneOf;

namespace TrueLayerPokedex.Application.Queries.GetBasicPokemonInfo
{
    public class GetBasicPokemonInfoQuery : IRequest<OneOf<PokemonInfoDto, ErrorDto>>
    {
        public string PokemonName { get; set; }
    }
}