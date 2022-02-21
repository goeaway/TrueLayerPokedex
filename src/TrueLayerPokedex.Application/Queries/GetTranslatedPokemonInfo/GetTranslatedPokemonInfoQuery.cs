using MediatR;
using TrueLayerPokedex.Domain.Dtos;
using OneOf;
using TrueLayerPokedex.Application.Common;
using TrueLayerPokedex.Domain.Models;

namespace TrueLayerPokedex.Application.Queries.GetTranslatedPokemonInfo
{
    public class GetTranslatedPokemonInfoQuery : ICacheableRequest<ResponseOrError<PokemonInfoDto>>
    {
        public string PokemonName { get; set; }
        public string CacheKey => $"translated:{PokemonName}";
    }
}