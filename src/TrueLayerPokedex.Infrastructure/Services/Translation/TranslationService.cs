using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using TrueLayerPokedex.Application.Common;
using TrueLayerPokedex.Domain.Models;

namespace TrueLayerPokedex.Infrastructure.Services.Translation
{
    public class TranslationService : ITranslationService
    {
        private readonly IEnumerable<ITranslator> _translators;

        public TranslationService(IEnumerable<ITranslator> translators)
        {
            _translators = translators;
        }

        public async Task<PokemonInfo> GetTranslationAsync(PokemonInfo pokemonInfo, CancellationToken cancellationToken)
        {
            Guard.Against.Null(pokemonInfo, nameof(pokemonInfo));

            var translator = _translators.FirstOrDefault(t => t.CanTranslate(pokemonInfo));

            if (translator == null)
            {
                return pokemonInfo;
            }

            var translatedDescription = 
                await translator.GetTranslationAsync(
                    pokemonInfo.Description, 
                    cancellationToken);

            return new PokemonInfo
            {
                Name = pokemonInfo.Name,
                Habitat = pokemonInfo.Habitat,
                IsLegendary = pokemonInfo.IsLegendary,
                Description = translatedDescription
            };
        }
    }
}