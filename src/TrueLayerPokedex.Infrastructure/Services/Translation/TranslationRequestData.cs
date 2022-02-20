using System.Text.Json.Serialization;

namespace TrueLayerPokedex.Infrastructure.Services.Translation
{
    public class TranslationRequestData
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}