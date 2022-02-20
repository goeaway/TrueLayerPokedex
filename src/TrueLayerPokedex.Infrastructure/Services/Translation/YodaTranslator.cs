using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using TrueLayerPokedex.Domain.Models;

namespace TrueLayerPokedex.Infrastructure.Services.Translation
{
    public class YodaTranslator : ITranslator
    {
        private readonly HttpClient _client;

        public YodaTranslator(HttpClient client)
        {
            _client = client;
        }

        public bool CanTranslate(PokemonInfo pokemonInfo)
        {
            Guard.Against.Null(pokemonInfo, nameof(pokemonInfo));

            return pokemonInfo.Habitat == "cave" || pokemonInfo.IsLegendary;
        }

        public async Task<PokemonInfo> GetTranslationAsync(PokemonInfo pokemonInfo, CancellationToken cancellationToken)
        {
            Guard.Against.Null(pokemonInfo, nameof(pokemonInfo));

            var content = new StringContent(JsonSerializer.Serialize(new TranslationRequestData
            {
                Text = pokemonInfo.Description
            }), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("yoda.json", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return pokemonInfo;
            }
            
            try
            {
                var responseContent = JsonSerializer.Deserialize<TranslationResponseData>(await response.Content.ReadAsStringAsync(cancellationToken));

                if (responseContent?.Contents?.Translated == null || responseContent.Contents.Translated == "")
                {
                    return pokemonInfo;
                }
                
                return new PokemonInfo
                {
                    Name = pokemonInfo.Name,
                    Habitat = pokemonInfo.Habitat,
                    Description = responseContent.Contents.Translated,
                    IsLegendary = pokemonInfo.IsLegendary
                };
            }
            catch (JsonException)
            {
                return pokemonInfo;
            }
        }
    }
}