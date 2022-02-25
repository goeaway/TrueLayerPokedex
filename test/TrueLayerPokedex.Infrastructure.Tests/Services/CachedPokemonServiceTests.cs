using System;
using System.Linq;
using System.Net;
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
    public class CachedPokemonServiceTests
    {
        private Mock<IPokemonService> _pokemonService;
        private Mock<IDistributedCache> _distributedCache;
        private TestNowProvider _nowProvider;
        private CachingOptions _cachingOptionsValue;
        private Mock<IOptionsSnapshot<CachingOptions>> _cachingOptions;
        private CachedPokemonService _sut;

        [SetUp]
        public void Init()
        {
            _pokemonService = new Mock<IPokemonService>();
            _distributedCache = new Mock<IDistributedCache>();
            _nowProvider = new TestNowProvider();
            
            _cachingOptionsValue = new CachingOptions
            {
                Ttl = TimeSpan.FromHours(1)
            };
            _cachingOptions = new Mock<IOptionsSnapshot<CachingOptions>>();
            _cachingOptions.Setup(mock => mock.Value)
                .Returns(_cachingOptionsValue);
            
            _sut = new CachedPokemonService(
                _pokemonService.Object, 
                _distributedCache.Object,
                _nowProvider,
                _cachingOptions.Object);
        }
        
        [Test]
        public void GetPokemonDataAsync_Throws_If_PokemonName_Null()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _sut.GetPokemonDataAsync(null, default));
        }
        
        [Test]
        [TestCase("")]
        [TestCase(" ")]
        public void GetPokemonDataAsync_Throws_If_PokemonName_Empty(string name)
        {
            Assert.ThrowsAsync<ArgumentException>(() => _sut.GetPokemonDataAsync(name, default));
        }

        [Test]
        public async Task GetPokemonDataAsync_Returns_Success_Data_From_Distributed_Cache_If_It_Exists_And_Does_Not_Use_PokemonService()
        {
            const string name = "mewtwo";
            const string description = "description";
            const string habitat = "habitat";
            const bool isLegendary = true;
            
            var cachedData = new PokemonInfo
            {
                Name = name,
                Description = description,
                Habitat = habitat,
                IsLegendary = isLegendary
            };

            _distributedCache.Setup(mock => mock.GetAsync($"basic:{name}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedData)));

            var result = await _sut.GetPokemonDataAsync(name, default);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.IsNull(result.Message);
            
            var data = result.Data;
            
            Assert.AreEqual(name, data.Name);
            Assert.AreEqual(description, data.Description);
            Assert.AreEqual(habitat, data.Habitat);
            Assert.AreEqual(isLegendary, data.IsLegendary);
            
            _pokemonService.Verify(
                mock => mock.GetPokemonDataAsync(
                    It.IsAny<string>(), 
                    It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        [Test]
        public async Task
            GetPokemonDataAsync_Returns_Success_Response_From_Pokemon_Service_If_Cache_Does_Not_Have_Data()
        {
            const string name = "mewtwo";
            const string description = "description";
            const string habitat = "habitat";
            const bool isLegendary = true;
            
            var pokemonServiceData = new PokemonInfo
            {
                Name = name,
                Description = description,
                Habitat = habitat,
                IsLegendary = isLegendary
            };

            _distributedCache.Setup(mock => mock.GetAsync($"basic:{name}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(null as byte[]);

            _pokemonService.Setup(mock => mock.GetPokemonDataAsync(name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PokemonServiceResponse
                {
                    Data = pokemonServiceData,
                    Success = true,
                    StatusCode = HttpStatusCode.OK
                });

            var result = await _sut.GetPokemonDataAsync(name, default);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.IsNull(result.Message);
            
            var data = result.Data;
            
            Assert.AreEqual(name, data.Name);
            Assert.AreEqual(description, data.Description);
            Assert.AreEqual(habitat, data.Habitat);
            Assert.AreEqual(isLegendary, data.IsLegendary);
        }
        
        [Test]
        public async Task
            GetPokemonDataAsync_Returns_Error_Response_From_Pokemon_Service_If_Cache_Does_Not_Have_Data_And_Error_Occurs()
        {
            const string name = "mewtwo";
            const HttpStatusCode errorCode = HttpStatusCode.NotFound;
            const string errorMessage = "error";
            
            _distributedCache.Setup(mock => mock.GetAsync($"basic:{name}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(null as byte[]);

            _pokemonService.Setup(mock => mock.GetPokemonDataAsync(name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PokemonServiceResponse
                {
                    Success = false,
                    StatusCode = errorCode,
                    Message = errorMessage
                });

            var result = await _sut.GetPokemonDataAsync(name, default);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(errorCode, result.StatusCode);
            Assert.AreEqual(errorMessage, result.Message);
            Assert.IsNull(result.Data);
        }
        
        [Test]
        public async Task
            GetPokemonDataAsync_Saves_Data_To_Cache_If_PokemonService_Returned_Success()
        {
            var now = new DateTime(2022, 02, 28, 09, 00, 00);
            _nowProvider.Update(now);
            
            const string name = "mewtwo";
            const string description = "description";
            const string habitat = "habitat";
            const bool isLegendary = true;
            
            var pokemonServiceData = new PokemonInfo
            {
                Name = name,
                Description = description,
                Habitat = habitat,
                IsLegendary = isLegendary
            };

            _distributedCache.Setup(mock => 
                    mock.GetAsync(
                        $"basic:{name}", 
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(null as byte[]);

            _pokemonService.Setup(mock => mock.GetPokemonDataAsync(name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PokemonServiceResponse
                {
                    Data = pokemonServiceData,
                    Success = true,
                    StatusCode = HttpStatusCode.OK
                });

            await _sut.GetPokemonDataAsync(name, default);

            var byteData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pokemonServiceData)); 

            _distributedCache.Verify(mock => mock.SetAsync(
                $"basic:{name}", 
                It.Is<byte[]>(value => value.Length == byteData.Length && value.First() == byteData.First() && value.Last() == byteData.Last()),
                It.Is<DistributedCacheEntryOptions>(
                    value => value.AbsoluteExpiration == _nowProvider.Now.Add(_cachingOptionsValue.Ttl)),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }
        
        [Test]
        public async Task
            GetPokemonDataAsync_Does_Not_Save_Data_To_Cache_If_PokemonService_Returned_Error()
        {
            var now = new DateTime(2022, 02, 28, 09, 00, 00);
            _nowProvider.Update(now);
            
            const string name = "mewtwo";
            const HttpStatusCode errorCode = HttpStatusCode.NotFound;
            const string errorMessage = "error";

            _distributedCache.Setup(mock => 
                    mock.GetAsync(
                        $"basic:{name}", 
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(null as byte[]);

            _pokemonService.Setup(mock => mock.GetPokemonDataAsync(name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PokemonServiceResponse
                {
                    Success = false,
                    StatusCode = errorCode,
                    Message = errorMessage
                });

            await _sut.GetPokemonDataAsync(name, default);

            _distributedCache.Verify(mock => mock.SetAsync(
                It.IsAny<string>(), 
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()
            ), Times.Never);
        }
    }
}