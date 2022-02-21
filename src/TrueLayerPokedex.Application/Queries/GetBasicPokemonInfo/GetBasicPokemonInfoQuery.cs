using TrueLayerPokedex.Domain.Dtos;
using OneOf;
using TrueLayerPokedex.Application.Common;
using TrueLayerPokedex.Domain.Models;

namespace TrueLayerPokedex.Application.Queries.GetBasicPokemonInfo
{
    public class GetBasicPokemonInfoQuery : ICacheableRequest<ResponseOrError<PokemonInfoDto>>
    {
        public string PokemonName { get; set; }
        public string CacheKey => $"basic:{PokemonName}";
    }
}