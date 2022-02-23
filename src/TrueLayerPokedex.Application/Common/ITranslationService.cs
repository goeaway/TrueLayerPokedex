using System.Threading;
using System.Threading.Tasks;
using TrueLayerPokedex.Domain.Models;

namespace TrueLayerPokedex.Application.Common
{
    /// <summary>
    /// Exposes functionality to translate properties for a given <see cref="PokemonInfo"/>
    /// </summary>
    public interface ITranslationService
    {
        Task<PokemonInfo> GetTranslationAsync(PokemonInfo pokemonInfo, CancellationToken cancellationToken);
    }
}