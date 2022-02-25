using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using TrueLayerPokedex.Application.Common;
using TrueLayerPokedex.Application.Queries.GetTranslatedPokemonInfo;
using TrueLayerPokedex.Domain.Models;

namespace TrueLayerPokedex.Application.Tests.Queries
{
    public class GetTranslatedPokemonInfoTests
    {
        private Mock<IPokemonService> _pokemonService;
        private Mock<ITranslationService> _translationService;
        private GetTranslatedPokemonInfoHandler _sut;

        [SetUp]
        public void Init()
        {
            _pokemonService = new Mock<IPokemonService>();
            _translationService = new Mock<ITranslationService>();
            
            _sut = new GetTranslatedPokemonInfoHandler(
                _pokemonService.Object, 
                _translationService.Object);
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
    }
}