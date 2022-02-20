using System.Threading;
using System.Threading.Tasks;
using TrueLayerPokedex.Domain.Models;

namespace TrueLayerPokedex.Infrastructure.Services.Translation
{
    public interface ITranslator
    {
        bool CanTranslate(PokemonInfo pokemonInfo);
        Task<PokemonInfo> GetTranslationAsync(PokemonInfo pokemonInfo, CancellationToken cancellationToken);
    }
}