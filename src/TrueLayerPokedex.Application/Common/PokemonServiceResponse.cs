using System.Net;
using TrueLayerPokedex.Domain.Dtos;

namespace TrueLayerPokedex.Application.Common
{
    public class PokemonServiceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public PokemonInfoDto Data { get; set; } 
    }
}