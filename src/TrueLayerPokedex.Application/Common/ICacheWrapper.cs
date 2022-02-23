using System;
using System.Threading;
using System.Threading.Tasks;

namespace TrueLayerPokedex.Application.Common
{
    /// <summary>
    /// Abstracts cache interactions for more straightforward use in handlers
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICacheWrapper<T> where T : class
    {
        Task<T> GetAsync(string key, CancellationToken cancellationToken);
        Task SetAsync(string key, T data, DateTime expiry, CancellationToken cancellationToken);
    }
}