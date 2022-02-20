using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using TrueLayerPokedex.Domain.Dtos;

namespace TrueLayerPokedex.Tests.Integration
{
    public class TranslatedPokemonEndpointTests
    {
        [Test]
        public async Task Can_Get_Data_From_PokeApi()
        {
            const string name = "mewtwo", 
                desc = "Created by a scientist after years of horrific gene splicing and dna engineering experiments,  it was.", 
                habitat = "rare";
            
            const bool isLegendary = true;

            var config = new Dictionary<string, string>
            {
                {"PokemonApi:BaseUrl", "https://pokeapi.co/api/v2/"},
                {"Translations:BaseUrl", "https://api.funtranslations.com/translate/"}
            };
            
            var (client, _) = Setup.CreateServer(config);

            var result = await client.GetAsync("/pokemon/translated/mewtwo");

            result.EnsureSuccessStatusCode();

            var resultContent = JsonConvert.DeserializeObject<PokemonInfoDto>(await result.Content.ReadAsStringAsync());
            
            Assert.AreEqual(name, resultContent.Name);
            Assert.AreEqual(habitat, resultContent.Habitat);
            Assert.AreEqual(desc, resultContent.Description);
            Assert.AreEqual(isLegendary, resultContent.IsLegendary);
        }
    }
}