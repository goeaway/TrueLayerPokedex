using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using TrueLayerPokedex.Domain.Dtos;

namespace TrueLayerPokedex.Tests.Integration
{
    public class BasicPokemonEndpointTests
    {
        [Test]
        public async Task Can_Get_Data_From_PokeApi()
        {
            const string name = "mewtwo", 
                desc = "It was created by a scientist after years of horrific gene splicing and DNA engineering experiments.", 
                habitat = "rare";
            
            const bool isLegendary = true;

            var config = new Dictionary<string, string>
            {
                {"PokemonApi:BaseUrl", "https://pokeapi.co/api/v2/"}
            };
            
            var (client, _) = Setup.CreateServer(config);

            var result = await client.GetAsync("/pokemon/mewtwo");

            result.EnsureSuccessStatusCode();

            var resultContent = JsonConvert.DeserializeObject<PokemonInfoDto>(await result.Content.ReadAsStringAsync());
            
            Assert.AreEqual(name, resultContent.Name);
            Assert.AreEqual(habitat, resultContent.Habitat);
            Assert.AreEqual(desc, resultContent.Description);
            Assert.AreEqual(isLegendary, resultContent.IsLegendary);
        }
    }
}