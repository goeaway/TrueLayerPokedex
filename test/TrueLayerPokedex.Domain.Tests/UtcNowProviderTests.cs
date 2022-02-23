using System;
using NUnit.Framework;

namespace TrueLayerPokedex.Domain.Tests
{
    public class UtcNowProviderTests
    {
        [Test]
        public void Ctor_Sets_Now_As_DateTime_UtcNow()
        {
            var provider = new UtcNowProvider();
            Assert.IsTrue(Math.Abs((DateTime.UtcNow - provider.Now).Ticks) <= TimeSpan.FromMilliseconds(1).Ticks);
        }
    }
}