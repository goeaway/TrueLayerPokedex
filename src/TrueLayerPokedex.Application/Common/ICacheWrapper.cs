using System;
using System.Threading;
using System.Threading.Tasks;

namespace TrueLayerPokedex.Application.Common
{
    public interface ICacheWrapper<T> where T : class
    {
        Task<T> GetAsync(string key, CancellationToken cancellationToken);
        Task SetAsync(string key, T data, DateTime expiry, CancellationToken cancellationToken);
    }
}