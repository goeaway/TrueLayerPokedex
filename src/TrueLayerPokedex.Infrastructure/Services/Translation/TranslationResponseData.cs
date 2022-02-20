using System.Text.Json.Serialization;

namespace TrueLayerPokedex.Infrastructure.Services.Translation
{
    internal class TranslationResponseData
    {
        [JsonPropertyName("success")]
        public SuccessData Success { get; set; }
        [JsonPropertyName("contents")]
        public ContentsData Contents { get; set; }
        
        public class SuccessData
        {
            [JsonPropertyName("total")]
            public int Total { get; set; }
        }

        public class ContentsData
        {
            [JsonPropertyName("translated")]
            public string Translated { get; set; }
            [JsonPropertyName("text")]
            public string Text { get; set; }
            [JsonPropertyName("translation")]
            public string Translation { get; set; }
        }
    }
}