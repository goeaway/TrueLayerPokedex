using System.Threading;
using System.Threading.Tasks;

namespace TrueLayerPokedex.Application.Common
{
    public interface IPokemonService
    {
        Task<PokemonServiceResponse> GetPokemonDataAsync(string pokemonName, CancellationToken cancellationToken);
    }
}