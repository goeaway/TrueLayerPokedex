using System;
using TrueLayerPokedex.Domain;

namespace TrueLayerPokedex.Application.Tests
{
    public class TestNowProvider : IUtcNowProvider
    {
        private DateTime? _now;
        public DateTime Now => _now ?? DateTime.UtcNow;

        public TestNowProvider()
        {
            
        }
        
        public TestNowProvider(DateTime now)
        {
            _now = now;
        }

        public void Update(DateTime now)
        {
            _now = now;
        }
        
    }
}