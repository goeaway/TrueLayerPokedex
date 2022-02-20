using System.Net;
using System.Text.Json.Serialization;

namespace TrueLayerPokedex.Domain.Dtos
{
    public class ErrorDto
    {
        [JsonIgnore]
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; }
    }
}