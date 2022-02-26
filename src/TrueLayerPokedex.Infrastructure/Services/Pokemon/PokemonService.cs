using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using TrueLayerPokedex.Application.Common;
using TrueLayerPokedex.Domain.Models;

namespace TrueLayerPokedex.Infrastructure.Services.Pokemon
{
    public class PokemonService : IPokemonService
    {
        private readonly HttpClient _client;

        public PokemonService(HttpClient client)
        {
            _client = client;
        }

        public async Task<PokemonServiceResponse> GetPokemonDataAsync(string pokemonName, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(pokemonName, nameof(pokemonName));

            var response = await _client.GetAsync($"pokemon-species/{pokemonName}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new PokemonServiceResponse
                {
                    Success = false,
                    Message = $"Could not get data from pokemon api, response indicates error: {response.StatusCode}",
                    StatusCode = response.StatusCode
                };
            }

            try
            {
                var responseContent = JsonSerializer.Deserialize<PokemonData>(await response.Content.ReadAsStringAsync(cancellationToken));

                return new PokemonServiceResponse
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Data = new PokemonInfo
                    {
                        Name = responseContent.Name,
                        IsLegendary = responseContent.IsLegendary.GetValueOrDefault(),
                        Habitat = responseContent.Habitat?.Name,
                        Description = ReplaceControlCharacters(responseContent.FlavorTextEntries?.FirstOrDefault(fte => fte.Language?.Name == "en")?.FlavorText)
                    }
                };
            }
            catch (JsonException)
            {
                return new PokemonServiceResponse
                {
                    Success = false,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Response content was in an unexpected format"
                };
            }

        }

        private static string ReplaceControlCharacters(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            return string.Join("", text.Select(c => char.IsControl(c) ? ' ' : c));
        }
    }
}