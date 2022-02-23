using System;

namespace TrueLayerPokedex.Domain
{
    /// <summary>
    /// Provides a Now property to be used to get a <see cref="DateTime"/> that represents the current date and time
    /// </summary>
    public interface IUtcNowProvider
    {
        public DateTime Now { get; }
    }
}