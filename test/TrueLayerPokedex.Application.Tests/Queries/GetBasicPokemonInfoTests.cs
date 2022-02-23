using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using TrueLayerPokedex.Application.Common;
using TrueLayerPokedex.Application.Queries.GetBasicPokemonInfo;
using TrueLayerPokedex.Domain.Dtos;
using TrueLayerPokedex.Domain.Models;
using TrueLayerPokedex.Domain.Options;

namespace TrueLayerPokedex.Application.Tests.Queries
{
    public class GetBasicPokemonInfoTests
    {
        private GetBasicPokemonInfoHandler _sut;
        private Mock<ICacheWrapper<PokemonInfoDto>> _cache;
        private Mock<IPokemonService> _pokemonService;
        private TestNowProvider _nowProvider;
        private CachingOptions _cachingOptionsValue;
        private Mock<IOptionsSnapshot<CachingOptions>> _cachingOptions;

        [SetUp]
        public void Init()
        {
            _pokemonService = new Mock<IPokemonService>();
            _cache = new Mock<ICacheWrapper<PokemonInfoDto>>();
            _cachingOptionsValue = new CachingOptions();
            _cachingOptions = new Mock<IOptionsSnapshot<CachingOptions>>();
            _cachingOptions.Setup(mock => mock.Value)
                .Returns(_cachingOptionsValue);
            _nowProvider = new TestNowProvider();
            
            _sut = new GetBasicPokemonInfoHandler(
                _pokemonService.Object,
                _cache.Object,
                _nowProvider,
                _cachingOptions.Object
            );
        }

        [Test]
        public async Task Handle_Should_Return_Cached_Version_Of_Data_If_Found_Before_Using_Pokemon_Service()
        {
            const string name = "mewtwo";
            const string description = "description";
            const string habitat = "habitat";
            const bool isLegendary = true;
            
            var query = new GetBasicPokemonInfoQuery
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

            _cache.Setup(mock => mock.GetAsync($"basic:{name}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedData);

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
            
            var query = new GetBasicPokemonInfoQuery
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
            
            var query = new GetBasicPokemonInfoQuery
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
        public async Task Handle_Should_Return_PokemonInfo_When_Returned_From_Pokemon_Service()
        {
            const string name = "mewtwo";
            const string description = "description";
            const string habitat = "habitat";
            const bool isLegendary = true;
            
            var query = new GetBasicPokemonInfoQuery
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
                
            var result = await _sut.Handle(query, default);

            var data = result.AsT0;
            
            Assert.AreEqual(name, data.Name);
            Assert.AreEqual(description, data.Description);
            Assert.AreEqual(habitat, data.Habitat);
            Assert.AreEqual(isLegendary, data.IsLegendary);
        }
        
        [Test]
        public async Task Handle_Should_Save_PokemonInfoDto_To_Cache_Before_Returning()
        {
            var now = new DateTime(2022, 02, 28, 09, 00, 00);
            _nowProvider.Update(now);
            _cachingOptionsValue.Ttl = TimeSpan.FromHours(1);
            
            const string name = "mewtwo";
            const string description = "description";
            const string habitat = "habitat";
            const bool isLegendary = true;
            
            var query = new GetBasicPokemonInfoQuery
            {
                PokemonName = name
            };

            var data = new PokemonInfo
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
                    Data = data 
                });

            _cache.Setup(
                mock => mock
                    .SetAsync(
                        It.IsAny<string>(), 
                        It.IsAny<PokemonInfoDto>(),
                        It.IsAny<DateTime>(), 
                        It.IsAny<CancellationToken>()));
                
            await _sut.Handle(query, default);

            _cache.Verify(
                mock => mock.SetAsync(
                    "basic:mewtwo", 
                    It.Is<PokemonInfoDto>(value => value.Name == name && value.Description == description && value.Habitat == habitat && value.IsLegendary == isLegendary),
                    now.Add(_cachingOptionsValue.Ttl),
                    It.IsAny<CancellationToken>()), 
                Times.Once);
        }
    }
}