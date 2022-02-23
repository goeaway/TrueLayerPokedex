using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using NUnit.Framework;
using TrueLayerPokedex.Domain.Dtos;
using TrueLayerPokedex.Infrastructure.Services.Caching;

namespace TrueLayerPokedex.Infrastructure.Tests.Services
{
    public class CacheWrapperTests
    {
        private Mock<IDistributedCache> _cache;
        private CacheWrapper<PokemonInfoDto> _sut;

        [SetUp]
        public void Init()
        {
            _cache = new Mock<IDistributedCache>();
            _sut = new CacheWrapper<PokemonInfoDto>(_cache.Object);
        }

        [Test]
        public void GetAsync_Throws_If_Key_Is_Null()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _sut.GetAsync(null, default));
        }
        
        [Test]
        [TestCase("")]
        [TestCase(" ")]
        public void GetAsync_Throws_If_Key_Is_Empty(string key)
        {
            Assert.ThrowsAsync<ArgumentException>(() => _sut.GetAsync(key, default));
        }
        
        [Test]
        public async Task GetAsync_Returns_Null_If_Nothing_In_Cache()
        {
            const string key = "cacheKey";

            _cache.Setup(mock =>
                    mock.GetAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(null as byte[]);

            var result = await _sut.GetAsync(key, default);
            
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetAsync_Returns_Item_From_Cache_If_There_Is_One()
        {
            const string key = "cacheKey";

            const string name = "name", desc = "desc", hab = "hab";
            const bool isLegendary = true;

            var toBeCached = new PokemonInfoDto
            {
                Name = name,
                Description = desc,
                Habitat = hab,
                IsLegendary = isLegendary
            };

            _cache.Setup(mock => 
                    mock.GetAsync(
                        It.IsAny<string>(), 
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(toBeCached)));

            var result = await _sut.GetAsync(key, default);
            
            Assert.AreEqual(name, result.Name);
            Assert.AreEqual(desc, result.Description);
            Assert.AreEqual(hab, result.Habitat);
            Assert.AreEqual(isLegendary, result.IsLegendary);
        }
        
        [Test]
        public async Task GetAsync_Returns_Null_If_Cache_Item_Could_Not_Be_Deserialised_To_Type()
        {
            const string key = "cacheKey";

            _cache.Setup(mock => 
                    mock.GetAsync(
                        It.IsAny<string>(), 
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(Encoding.UTF8.GetBytes("non json data"));

            var result = await _sut.GetAsync(key, default);

            Assert.IsNull(result);
        }

        [Test]
        public void SetAsync_Throws_If_Key_Is_Null()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _sut.SetAsync(null, new PokemonInfoDto(), new DateTime(), default));
        }
        
        [Test]
        [TestCase("")]
        [TestCase(" ")]
        public void SetAsync_Throws_If_Key_Is_Empty(string key)
        {
            Assert.ThrowsAsync<ArgumentException>(() => _sut.SetAsync(key, new PokemonInfoDto(), new DateTime(), default));
        }

        [Test]
        public void SetAsync_Throws_If_Data_Is_Null()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _sut.SetAsync("key", null, new DateTime(), default));
        }
        
        [Test]
        public async Task SetAsync_Uses_Cache_And_Provides_It_Byte_Version_Of_Data_And_AbsoluteExpiration()
        {
            const string key = "cacheKey";

            const string name = "name", desc = "desc", hab = "hab";
            const bool isLegendary = true;

            var expiry = new DateTime(2022, 02, 28);

            var toBeCached = new PokemonInfoDto
            {
                Name = name,
                Description = desc,
                Habitat = hab,
                IsLegendary = isLegendary
            };

            await _sut.SetAsync(key, toBeCached, expiry, default);

            var byteData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(toBeCached)); 

            _cache.Verify(
                mock => mock.SetAsync(
                    key, 
                    It.Is<byte[]>(value => value.Length == byteData.Length && value.First() == byteData.First() && value.Last() == byteData.Last()),
                    It.Is<DistributedCacheEntryOptions>(value =>
                        value.AbsoluteExpiration == expiry
                    ), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);
        }
    }
}