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
using TrueLayerPokedex.Application.Queries.GetBasicPokemonInfo;
using TrueLayerPokedex.Application.Queries.GetTranslatedPokemonInfo;
using TrueLayerPokedex.Domain.Dtos;
using TrueLayerPokedex.Domain.Models;
using TrueLayerPokedex.Domain.Options;

namespace TrueLayerPokedex.Application.Tests.Queries
{
    public class GetTranslatedPokemonInfoTests
    {
        private Mock<IPokemonService> _pokemonService;
        private Mock<ITranslationService> _translationService;
        private Mock<IDistributedCache> _cache;
        private TestNowProvider _nowProvider;
        private CachingOptions _cachingOptionsValue;
        private Mock<IOptionsSnapshot<CachingOptions>> _cachingOptions;
        
        private GetTranslatedPokemonInfoHandler _sut;

        [SetUp]
        public void Init()
        {
            _pokemonService = new Mock<IPokemonService>();
            _translationService = new Mock<ITranslationService>();
            
            _cache = new Mock<IDistributedCache>();
            _cachingOptionsValue = new CachingOptions();
            _cachingOptions = new Mock<IOptionsSnapshot<CachingOptions>>();
            _cachingOptions.Setup(mock => mock.Value)
                .Returns(_cachingOptionsValue);
            _nowProvider = new TestNowProvider();
            
            _sut = new GetTranslatedPokemonInfoHandler(
                _pokemonService.Object, 
                _translationService.Object,
                _cache.Object, _nowProvider, _cachingOptions.Object);
        }

        [Test]
        public async Task Handle_Should_Return_Cached_Version_Of_Data_If_Found_Before_Using_Pokemon_Service()
        {
            const string name = "mewtwo";
            const string description = "description";
            const string habitat = "habitat";
            const bool isLegendary = true;
            
            var query = new GetTranslatedPokemonInfoQuery
            {
                PokemonName = name
            };

            var cachedData = new PokemonInfoDto
            {
                Name = name,
                Description = description,
                Habitat = habitat,
                IsLegendary = isLegendary
            };

            _cache.Setup(mock => mock.GetAsync($"translated:{name}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedData)));

            var result = await _sut.Handle(query, default);

            var data = result.AsT0;
            
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
        public async Task Handle_Should_Use_Pokemon_Service_To_Get_Data_For_Pokemon_By_Providing_Pokemon_Name()
        {
            const string expectedPokemonName = "mewtwo";
            
            var query = new GetTranslatedPokemonInfoQuery
            {
                PokemonName = expectedPokemonName
            };

            _pokemonService
                .Setup(mock => mock.GetPokemonDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PokemonServiceResponse
                {
                    Success = false
                });
                
            await _sut.Handle(query, default);

            _pokemonService
                .Verify(mock => mock.GetPokemonDataAsync(expectedPokemonName, It.IsAny<CancellationToken>()),
                    Times.Once);
        }

        [Test]
        public async Task Handle_Should_Return_Error_Response_If_Pokemon_Service_Has_Error()
        {
            const HttpStatusCode expectedErrorCode = HttpStatusCode.NotFound;
            const string expectedErrorMessage = "error"; 
            
            var query = new GetTranslatedPokemonInfoQuery
            {
                PokemonName = "mewtwo"
            };

            _pokemonService
                .Setup(mock => mock.GetPokemonDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PokemonServiceResponse
                {
                    Success = false,
                    StatusCode = expectedErrorCode,
                    Message = expectedErrorMessage
                });
                
            var result = await _sut.Handle(query, default);

            var error = result.AsT1;
            
            Assert.AreEqual(expectedErrorCode, error.StatusCode);
            Assert.AreEqual(expectedErrorMessage, error.Message);
        }

        [Test]
        public async Task Handle_Should_Use_Translation_Service_To_Get_Translated_Version_Of_Pokemon_Data()
        {
            const string name = "mewtwo";
            const string description = "description";
            const string habitat = "habitat";
            const bool isLegendary = true;
            
            var query = new GetTranslatedPokemonInfoQuery
            {
                PokemonName = name
            };

            var pokemonInfo = new PokemonInfo
            {
                Name = name,
                IsLegendary = isLegendary,
                Habitat = habitat,
                Description = description
            }; 

            _pokemonService
                .Setup(mock => mock.GetPokemonDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PokemonServiceResponse
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Data = pokemonInfo 
                });
            
            _translationService
                .Setup(mock => mock.GetTranslationAsync(It.IsAny<PokemonInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(pokemonInfo);
                
            await _sut.Handle(query, default);

            _translationService
                .Verify(mock => mock.GetTranslationAsync(pokemonInfo, It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Test]
        public async Task Handle_Returns_Success_Response_With_Data_From_Translated_Service()
        {
            const string name = "name", description = "description", habitat = "habitat";
            const bool isLegendary = true;
            const string translatedName = "translated name", translatedDescription = "translated description", translatedHabitat = "translated habitat";
            
            var query = new GetTranslatedPokemonInfoQuery
            {
                PokemonName = name
            };

            var pokemonInfo = new PokemonInfo
            {
                Name = name,
                IsLegendary = isLegendary,
                Habitat = habitat,
                Description = description
            }; 

            _pokemonService
                .Setup(mock => mock.GetPokemonDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PokemonServiceResponse
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Data = pokemonInfo 
                });
            
            _translationService
                .Setup(mock => mock.GetTranslationAsync(It.IsAny<PokemonInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PokemonInfo
                {
                    Name = translatedName,
                    IsLegendary = isLegendary,
                    Habitat = translatedHabitat,
                    Description = translatedDescription
                });
                
            var result = await _sut.Handle(query, default);

            var data = result.AsT0;
            
            Assert.AreEqual(translatedName, data.Name);
            Assert.AreEqual(translatedDescription, data.Description);
            Assert.AreEqual(translatedHabitat, data.Habitat);
            Assert.AreEqual(isLegendary, data.IsLegendary);
        }
        
        [Test]
        public async Task Handle_Should_Save_PokemonInfoDto_To_Cache_Before_Returning()
        {
            var now = new DateTime(2022, 02, 28, 09, 00, 00);
            _nowProvider.Update(now);
            _cachingOptionsValue.Ttl = TimeSpan.FromHours(1);
            
            const string name = "mewtwo", description = "description", habitat = "habitat";
            const bool isLegendary = true;
            
            const string translatedName = "translated name", translatedDescription = "translated description", translatedHabitat = "translated habitat";
            
            var query = new GetTranslatedPokemonInfoQuery
            {
                PokemonName = name
            };

            _pokemonService
                .Setup(mock => mock.GetPokemonDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PokemonServiceResponse
                {
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Data = new PokemonInfo
                    {
                        Name = name,
                        IsLegendary = isLegendary,
                        Habitat = habitat,
                        Description = description
                    }
                });
            
            _translationService
                .Setup(mock => mock.GetTranslationAsync(It.IsAny<PokemonInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PokemonInfo
                {
                    Name = translatedName,
                    IsLegendary = isLegendary,
                    Habitat = translatedHabitat,
                    Description = translatedDescription
                });

            _cache.Setup(
                mock => mock
                    .SetAsync(
                        It.IsAny<string>(), 
                        It.IsAny<byte[]>(),
                        It.IsAny<DistributedCacheEntryOptions>(), 
                        It.IsAny<CancellationToken>()));
                
            await _sut.Handle(query, default);

            var byteData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new PokemonInfoDto
            {
                Name = translatedName,
                IsLegendary = isLegendary,
                Habitat = translatedHabitat,
                Description = translatedDescription
            })); 

            _cache.Verify(
                mock => mock.SetAsync(
                    "translated:mewtwo", 
                    It.Is<byte[]>(value => value.Length == byteData.Length && value.First() == byteData.First() && value.Last() == byteData.Last()),
                    It.Is<DistributedCacheEntryOptions>(value =>
                        value.AbsoluteExpiration == now.Add(_cachingOptionsValue.Ttl)
                    ), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);
        }
    }
}