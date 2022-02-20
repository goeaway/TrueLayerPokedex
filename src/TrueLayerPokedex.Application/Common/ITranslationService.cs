using System.Threading;
using System.Threading.Tasks;
using TrueLayerPokedex.Domain.Models;

namespace TrueLayerPokedex.Application.Common
{
    public interface ITranslationService
    {
        Task<PokemonInfo> GetTranslationAsync(PokemonInfo pokemonInfo, CancellationToken cancellationToken);
    }
}