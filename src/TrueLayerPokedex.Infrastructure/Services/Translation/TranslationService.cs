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
        // We take in a collection of translators, which are then looped through to find
        // one that can translate a given PokemonInfo.
        // The order is preserved, which allows the registration of these translators to dictate which
        // one is used first, should there be multiple translators that can handle a PokemonInfo
        private readonly IEnumerable<ITranslator> _translators;

        public TranslationService(IEnumerable<ITranslator> translators)
        {
            _translators = translators;
        }

        public async Task<PokemonInfo> GetTranslationAsync(PokemonInfo pokemonInfo, CancellationToken cancellationToken)
        {
            Guard.Against.Null(pokemonInfo, nameof(pokemonInfo));

            var translator = _translators.FirstOrDefault(t => t.CanTranslate(pokemonInfo));

            // It's possible no translators were provided or none can translate the info
            // so we return the input if that's the case
            if (translator == null)
            {
                return pokemonInfo;
            }

            // Translators could return the same description they were given
            var translatedDescription = 
                await translator.GetTranslationAsync(
                    pokemonInfo.Description, 
                    cancellationToken);

            // return a new instance to preserve the inputted one
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