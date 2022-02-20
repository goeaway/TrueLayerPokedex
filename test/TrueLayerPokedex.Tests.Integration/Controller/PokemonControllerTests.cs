using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using TrueLayerPokedex.Application.Common;
using TrueLayerPokedex.Domain.Dtos;

namespace TrueLayerPokedex.Tests.Integration.Controller
{
    public class PokemonControllerTests
    {
        [Test]
        public async Task Returns_404_If_PokemonName_Not_Provided()
        {
            var (client, _) = Setup.CreateServer(services => {});

            var result = await client.GetAsync("/pokemon/");
            
            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
        }
        
        [Test]
        public async Task Returns_404_If_Pokemon_Service_Returns_404()
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
        public async Task Returns_200_And_Content_If_Pokemon_Service_Returns_Content()
        {
            const string name = "name", desc = "desc", habitat = "hab";
            const bool isLegendary = true;
            
            var mockPokemonService = new Mock<IPokemonService>();

            mockPokemonService.Setup(mock => mock.GetPokemonDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PokemonServiceResponse
                {
                    Success = true,
                    Data = new PokemonInfoDto
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

            var result = await client.GetAsync("/pokemon/unknown");

            result.EnsureSuccessStatusCode();

            var resultContent = JsonConvert.DeserializeObject<PokemonInfoDto>(await result.Content.ReadAsStringAsync());
            
            Assert.AreEqual(name, resultContent.Name);
            Assert.AreEqual(habitat, resultContent.Habitat);
            Assert.AreEqual(desc, resultContent.Description);
            Assert.AreEqual(isLegendary, resultContent.IsLegendary);
        }
    }
}