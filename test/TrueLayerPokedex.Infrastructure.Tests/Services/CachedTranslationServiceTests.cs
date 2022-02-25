using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using TrueLayerPokedex.Application.Common;
using TrueLayerPokedex.Domain.Models;
using TrueLayerPokedex.Domain.Options;
using TrueLayerPokedex.Infrastructure.Services.Caching;

namespace TrueLayerPokedex.Infrastructure.Tests.Services
{
    public class CachedTranslationServiceTests
    {
        private Mock<ITranslationService> _translationService;
        private Mock<IDistributedCache> _distributedCache;
        private TestNowProvider _nowProvider;
        private CachingOptions _cachingOptionsValue;
        private Mock<IOptionsSnapshot<CachingOptions>> _cachingOptions;
        private CachedTranslationService _sut;

        [SetUp]
        public void Init()
        {
            _translationService = new Mock<ITranslationService>();
            _distributedCache = new Mock<IDistributedCache>();
            _nowProvider = new TestNowProvider();
            
            _cachingOptionsValue = new CachingOptions();
            _cachingOptions = new Mock<IOptionsSnapshot<CachingOptions>>();
            _cachingOptions.Setup(mock => mock.Value)
                .Returns(_cachingOptionsValue);
            
            _sut = new CachedTranslationService(
                _translationService.Object,
                _distributedCache.Object,
                _nowProvider,
                _cachingOptions.Object
            );
        }
        
        [Test]
        public void GetTranslationAsync_Throws_If_PokemonInfo_Null()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _sut.GetTranslationAsync(null, default));
        }
        
        [Test]
        public void GetPokemonDataAsync_Throws_If_PokemonInfo_PokemonName_Null()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _sut.GetTranslationAsync(new PokemonInfo { Name = null }, default));
        }
        
        [Test]
        [TestCase("")]
        [TestCase(" ")]
        public void GetPokemonDataAsync_Throws_If_PokemonInfo_PokemonName_Empty(string name)
        {
            Assert.ThrowsAsync<ArgumentException>(() => _sut.GetTranslationAsync(new PokemonInfo { Name = name }, default));
        }
        
        [Test]
        public async Task GetTranslationAsync_Returns_Data_From_Distributed_Cache_If_It_Exists_And_Does_Not_Use_TranslationService()
        {
            const string name = "mewtwo", description = "description", habitat = "habitat";
            const bool isLegendary = true;

            const string translatedDescription = "translatedDescription";
            
            var inputData = new PokemonInfo
            {
                Name = name,
                Description = description,
                Habitat = habitat,
                IsLegendary = isLegendary
            };
            
            var cachedData = new PokemonInfo
            {
                Name = name,
                Description = translatedDescription,
                Habitat = habitat,
                IsLegendary = isLegendary
            };

            _distributedCache.Setup(mock => mock.GetAsync($"translated:{name}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedData)));

            var result = await _sut.GetTranslationAsync(inputData, default);

            Assert.AreEqual(name, result.Name);
            Assert.AreEqual(translatedDescription, result.Description);
            Assert.AreEqual(habitat, result.Habitat);
            Assert.AreEqual(isLegendary, result.IsLegendary);
            
            _translationService.Verify(
                mock => mock.GetTranslationAsync(
                    It.IsAny<PokemonInfo>(), 
                    It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        [Test]
        public async Task
            GetTranslationAsync_Returns_Response_From_TranslationService_If_Cache_Does_Not_Have_Data()
        {
            const string name = "mewtwo", description = "description", habitat = "habitat";
            const bool isLegendary = true;

            const string translatedDescription = "translatedDescription";
            
            var inputData = new PokemonInfo
            {
                Name = name,
                Description = description,
                Habitat = habitat,
                IsLegendary = isLegendary
            };
            
            var returnedData = new PokemonInfo
            {
                Name = name,
                Description = translatedDescription,
                Habitat = habitat,
                IsLegendary = isLegendary
            };
            
            _distributedCache.Setup(mock => mock.GetAsync($"translated:{name}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(null as byte[]);

            _translationService.Setup(mock => mock.GetTranslationAsync(inputData, It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnedData);

            var result = await _sut.GetTranslationAsync(inputData, default);

            Assert.AreEqual(name, result.Name);
            Assert.AreEqual(translatedDescription, result.Description);
            Assert.AreEqual(habitat, result.Habitat);
            Assert.AreEqual(isLegendary, result.IsLegendary);
        }
        
        [Test]
        public async Task
            GetTranslationAsync_Saves_Data_From_TranslationService_To_Cache()
        {
            var now = new DateTime(2022, 02, 28, 09, 00, 00);
            _nowProvider.Update(now);
            
            const string name = "mewtwo", description = "description", habitat = "habitat";
            const bool isLegendary = true;

            const string translatedDescription = "translatedDescription";
            
            var inputData = new PokemonInfo
            {
                Name = name,
                Description = description,
                Habitat = habitat,
                IsLegendary = isLegendary
            };
            
            var returnedData = new PokemonInfo
            {
                Name = name,
                Description = translatedDescription,
                Habitat = habitat,
                IsLegendary = isLegendary
            };
            
            _distributedCache.Setup(mock => 
                    mock.GetAsync(
                        $"translated:{name}", 
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(null as byte[]);

            _translationService.Setup(mock => mock.GetTranslationAsync(inputData, It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnedData);

            await _sut.GetTranslationAsync(inputData, default);

            var byteData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(returnedData)); 

            _distributedCache.Verify(mock => mock.SetAsync(
                $"translated:{name}", 
                It.Is<byte[]>(value => value.Length == byteData.Length && value.First() == byteData.First() && value.Last() == byteData.Last()),
                It.Is<DistributedCacheEntryOptions>(
                    value => value.AbsoluteExpiration == _nowProvider.Now.Add(_cachingOptionsValue.Ttl)),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }
    }
}