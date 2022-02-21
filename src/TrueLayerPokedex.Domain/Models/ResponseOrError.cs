using OneOf;
using TrueLayerPokedex.Domain.Dtos;

namespace TrueLayerPokedex.Domain.Models
{
    public class ResponseOrError<TResponse> : OneOfBase<TResponse, ErrorDto>
    {
        public ResponseOrError(TResponse input) : base (input) {}
        protected ResponseOrError(OneOf<TResponse, ErrorDto> input) : base(input){}
        
        public static implicit operator ResponseOrError<TResponse>(TResponse _) => new (_);
        public static implicit operator ResponseOrError<TResponse>(ErrorDto _) => new (_);
    }
}