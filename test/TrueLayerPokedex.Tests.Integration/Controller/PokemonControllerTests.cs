using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using TrueLayerPokedex.Application.Common;
using TrueLayerPokedex.Domain.Dtos;
using TrueLayerPokedex.Domain.Models;

namespace TrueLayerPokedex.Tests.Integration.Controller
{
    public class PokemonControllerTests
    {
        [Test]
        public async Task Basic_Returns_404_If_PokemonName_Not_Provided()
        {
            var (client, _) = Setup.CreateServer(_ => {});

            var result = await client.GetAsync("/pokemon/");
            
            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
        }
        
        [Test]
        public async Task Basic_Returns_404_If_Pokemon_Service_Returns_404()
        {
            const HttpStatusCode errorCode = HttpStatusCode.NotFound;
            
            var mockPokemonService = new Mock<IPokemonService>();

            mockPokemonService.Setup(mock => mock.GetPokemonDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PokemonServiceResponse
                {
                    StatusCode = errorCode,
                });
            
            var (client, _) = Setup.CreateServer(services =>
            {
                services.AddSingleton(mockPokemonService.Object);
            });

            var result = await client.GetAsync("/pokemon/unknown");
            
            Assert.AreEqual(errorCode, result.StatusCode);
        }
        
        [Test]
        public async Task Basic_Returns_200_And_Content_If_Pokemon_Service_Returns_Content()
        {
            const string name = "name", desc = "desc", habitat = "hab";
            const bool isLegendary = true;
            
            var mockPokemonService = new Mock<IPokemonService>();

            mockPokemonService.Setup(mock => mock.GetPokemonDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PokemonServiceResponse
                {
                    Success = true,
                    Data = new PokemonInfo
                    {
                        Name = name,
                        Description = desc,
                        Habitat = habitat,
                        IsLegendary = isLegendary
                    }
                });
            
            var (client, _) = Setup.CreateServer(services =>
            {
                services.AddSingleton(mockPokemonService.Object);
            });

            var result = await client.GetAsync("/pokemon/apokemon");

            result.EnsureSuccessStatusCode();

            var resultContent = JsonConvert.DeserializeObject<PokemonInfoDto>(await result.Content.ReadAsStringAsync());
            
            Assert.AreEqual(name, resultContent.Name);
            Assert.AreEqual(habitat, resultContent.Habitat);
            Assert.AreEqual(desc, resultContent.Description);
            Assert.AreEqual(isLegendary, resultContent.IsLegendary);
        }
        
        [Test]
        public async Task Translation_Returns_404_If_Pokemon_Service_Returns_404()
        {
            const HttpStatusCode errorCode = HttpStatusCode.NotFound;
            
            var mockPokemonService = new Mock<IPokemonService>();

            mockPokemonService.Setup(mock => mock.GetPokemonDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PokemonServiceResponse
                {
                    StatusCode = errorCode,
                });

            var mockTranslationService = new Mock<ITranslationService>();
            
            var (client, _) = Setup.CreateServer(services =>
            {
                services.AddSingleton(mockPokemonService.Object);
                services.AddSingleton(mockTranslationService.Object);
            });

            var result = await client.GetAsync("/pokemon/translated/unknown");
            
            Assert.AreEqual(errorCode, result.StatusCode);
        }
        
        [Test]
        public async Task Translation_Returns_200_And_Content_If_Pokemon_Service_Returns_Content()
        {
            const string name = "name", desc = "desc", habitat = "hab";
            const bool isLegendary = true;

            const string translatedDesc = "trans desc";
            
            var mockPokemonService = new Mock<IPokemonService>();

            mockPokemonService.Setup(mock => mock.GetPokemonDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PokemonServiceResponse
                {
                    Success = true,
                    Data = new PokemonInfo
                    {
                        Name = name,
                        Description = desc,
                        Habitat = habitat,
                        IsLegendary = isLegendary
                    }
                });

            var mockTranslationService = new Mock<ITranslationService>();

            mockTranslationService.Setup(mock =>
                    mock.GetTranslationAsync(It.IsAny<PokemonInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PokemonInfo
                {
                    Name = name,
                    Habitat = habitat,
                    IsLegendary = isLegendary,
                    Description = translatedDesc
                });
            
            var (client, _) = Setup.CreateServer(services =>
            {
                services.AddSingleton(mockPokemonService.Object);
                services.AddSingleton(mockTranslationService.Object);
            });

            var result = await client.GetAsync("/pokemon/translated/apokemon");

            result.EnsureSuccessStatusCode();

            var resultContent = JsonConvert.DeserializeObject<PokemonInfoDto>(await result.Content.ReadAsStringAsync());
            
            Assert.AreEqual(name, resultContent.Name);
            Assert.AreEqual(habitat, resultContent.Habitat);
            Assert.AreEqual(translatedDesc, resultContent.Description);
            Assert.AreEqual(isLegendary, resultContent.IsLegendary);
        }
    }
}