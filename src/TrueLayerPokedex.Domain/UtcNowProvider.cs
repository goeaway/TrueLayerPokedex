using System;

namespace TrueLayerPokedex.Domain
{
    public class UtcNowProvider : IUtcNowProvider
    {
        public DateTime Now => DateTime.UtcNow;
    }
}