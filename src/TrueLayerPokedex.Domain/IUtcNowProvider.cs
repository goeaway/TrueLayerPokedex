using System;

namespace TrueLayerPokedex.Domain
{
    public interface IUtcNowProvider
    {
        public DateTime Now { get; }
    }
}