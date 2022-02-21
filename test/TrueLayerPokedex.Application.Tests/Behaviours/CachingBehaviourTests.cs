using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using TrueLayerPokedex.Application.Behaviours;
using TrueLayerPokedex.Application.Queries.GetBasicPokemonInfo;
using TrueLayerPokedex.Domain.Dtos;

using TrueLayerPokedex.Domain.Models;
using TrueLayerPokedex.Domain.Options;

namespace TrueLayerPokedex.Application.Tests.Behaviours
{
    public class CachingBehaviourTests
    {
        private Mock<IDistributedCache> _distributedCache;
        private Mock<IOptionsSnapshot<CachingOptions>> _cachingOptions;
        private Mock<RequestHandlerDelegate<ResponseOrError<PokemonInfoDto>>> _delegateMock;
        private TestNowProvider _nowProvider;
        private CachingOptions _cachingOptionsValue;
        private CachingBehaviour<GetBasicPokemonInfoQuery, ResponseOrError<PokemonInfoDto>> _sut;

        [SetUp]
        public void Init()
        {
            _distributedCache = new Mock<IDistributedCache>();
            _cachingOptionsValue = new CachingOptions();
            _cachingOptions = new Mock<IOptionsSnapshot<CachingOptions>>();
            _cachingOptions.Setup(mock => mock.Value)
                .Returns(_cachingOptionsValue);
            _nowProvider = new TestNowProvider();

            _delegateMock = new Mock<RequestHandlerDelegate<ResponseOrError<PokemonInfoDto>>>();
            
            _sut = new CachingBehaviour<GetBasicPokemonInfoQuery, ResponseOrError<PokemonInfoDto>>(
                _distributedCache.Object,
                _nowProvider,
                _cachingOptions.Object
            );
        }

        [Test]
        public async Task Handle_Returns_Data_Found_In_GetAsync_If_It_Exists()
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

            _distributedCache.Setup(mock => mock.GetAsync($"basic:{name}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedData)));

            var result = await _sut.Handle(query, default, _delegateMock.Object);

            var data = result.AsT0;
            
            Assert.AreEqual(name, data.Name);
            Assert.AreEqual(description, data.Description);
            Assert.AreEqual(habitat, data.Habitat);
            Assert.AreEqual(isLegendary, data.IsLegendary);
            
            _delegateMock.Verify(
                mock => mock(), 
                Times.Never);
        }
        
        [Test]
        public async Task Handle_Returns_Result_From_Delegate_If_No_Cache_Entry_Found()
        {
            const string name = "mewtwo";
            const string description = "description";
            const string habitat = "habitat";
            const bool isLegendary = true;
            
            var query = new GetBasicPokemonInfoQuery
            {
                PokemonName = name
            };

            _distributedCache
                .Setup(mock => mock.GetAsync($"basic:{name}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(null as byte[]);

            _delegateMock.Setup(mock => mock())
                .ReturnsAsync(new PokemonInfoDto
                {
                    Name = name,
                    Description = description,
                    Habitat = habitat,
                    IsLegendary = isLegendary
                });

            var result = await _sut.Handle(query, default, _delegateMock.Object);

            var data = result.AsT0;
            
            Assert.AreEqual(name, data.Name);
            Assert.AreEqual(description, data.Description);
            Assert.AreEqual(habitat, data.Habitat);
            Assert.AreEqual(isLegendary, data.IsLegendary);
            
            _delegateMock.Verify(
                mock => mock(), 
                Times.Once);
        }
        
        // sets data with setasync and key after handler call
        [Test]
        public async Task Handle_Saves_Data_From_Delegate_In_Cache()
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

            _distributedCache
                .Setup(mock => mock.GetAsync($"basic:{name}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(null as byte[]);

            _delegateMock.Setup(mock => mock())
                .ReturnsAsync(new PokemonInfoDto
                {
                    Name = name,
                    Description = description,
                    Habitat = habitat,
                    IsLegendary = isLegendary
                });

            await _sut.Handle(query, default, _delegateMock.Object);

            var byteData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new PokemonInfoDto
            {
                Name = name,
                IsLegendary = isLegendary,
                Habitat = habitat,
                Description = description
            })); 

            _distributedCache.Verify(
                mock => mock.SetAsync(
                    "basic:mewtwo", 
                    It.Is<byte[]>(value => value.Length == byteData.Length && value.First() == byteData.First() && value.Last() == byteData.Last()),
                    It.Is<DistributedCacheEntryOptions>(value =>
                        value.AbsoluteExpiration == now.Add(_cachingOptionsValue.Ttl)
                    ), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);
        }
    }
}