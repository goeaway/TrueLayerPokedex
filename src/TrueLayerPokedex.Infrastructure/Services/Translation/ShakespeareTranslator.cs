using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using TrueLayerPokedex.Domain.Models;

namespace TrueLayerPokedex.Infrastructure.Services.Translation
{
    public class ShakespeareTranslator : ITranslator
    {
        private readonly HttpClient _client;

        public ShakespeareTranslator(HttpClient client)
        {
            _client = client;
        }

        public bool CanTranslate(PokemonInfo pokemonInfo) => true;

        public async Task<string> GetTranslationAsync(string description, CancellationToken cancellationToken)
        {
            if (description == null)
            {
                return null;
            }

            var content = new StringContent(JsonSerializer.Serialize(new TranslationRequestData
            {
                Text = description
            }), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("shakespeare.json", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return description;
            }
            
            try
            {
                var responseContent = JsonSerializer.Deserialize<TranslationResponseData>(await response.Content.ReadAsStringAsync(cancellationToken));

                if (responseContent?.Contents?.Translated == null || responseContent.Contents.Translated == "")
                {
                    return description;
                }

                return responseContent.Contents.Translated;
            }
            catch (JsonException)
            {
                return description;
            }
        }
    }
}