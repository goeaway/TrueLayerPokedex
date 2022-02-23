using System.Threading;
using System.Threading.Tasks;

namespace TrueLayerPokedex.Application.Common
{
    /// <summary>
    /// Exposes functionality to interact with the PokeAPI
    /// </summary>
    public interface IPokemonService
    {
        Task<PokemonServiceResponse> GetPokemonDataAsync(string pokemonName, CancellationToken cancellationToken);
    }
}