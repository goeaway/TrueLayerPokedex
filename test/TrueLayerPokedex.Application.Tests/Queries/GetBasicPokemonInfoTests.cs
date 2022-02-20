using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using TrueLayerPokedex.Application.Common;
using TrueLayerPokedex.Application.Queries.GetBasicPokemonInfo;
using TrueLayerPokedex.Domain.Dtos;

namespace TrueLayerPokedex.Application.Tests.Queries
{
    public class GetBasicPokemonInfoTests
    {
        private GetBasicPokemonInfoHandler _sut;
        private Mock<IPokemonService> _pokemonService;

        [SetUp]
        public void Init()
        {
            _pokemonService = new Mock<IPokemonService>();
            
            _sut = new GetBasicPokemonInfoHandler(
                _pokemonService.Object    
            );
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
                    Data = new PokemonInfoDto
                    {
                        Name = name,
                        IsLegendary = true,
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
    }
}