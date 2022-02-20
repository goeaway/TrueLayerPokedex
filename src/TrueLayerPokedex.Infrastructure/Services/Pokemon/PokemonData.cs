using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TrueLayerPokedex.Infrastructure.Services.Pokemon
{
    internal class PokemonData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("flavor_text_entries")]
        public IReadOnlyCollection<FlavorTextEntry> FlavorTextEntries { get; set; }
        
        [JsonPropertyName("habitat")]
        public HabitatData Habitat { get; set; }
        
        [JsonPropertyName("is_legendary")]
        public bool IsLegendary { get; set; }
        
        public class Language
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
        }
        
        public class FlavorTextEntry
        {
            [JsonPropertyName("flavor_text")]
            public string FlavorText { get; set; }
            [JsonPropertyName("language")]
            public Language Language { get; set; }
        }

        public class HabitatData
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
        }
    }
}