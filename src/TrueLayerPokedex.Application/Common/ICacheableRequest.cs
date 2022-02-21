using System;
using MediatR;

namespace TrueLayerPokedex.Application.Common
{
    public interface ICacheableRequest<TResponse> : IRequest<TResponse>
    {
        string CacheKey { get; }
    }
}