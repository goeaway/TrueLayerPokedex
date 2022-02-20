using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using TrueLayerPokedex.Infrastructure.Services.Pokemon;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace TrueLayerPokedex.Infrastructure.Tests.Services
{
    public class PokemonServiceTests
    {
        private PokemonService _sut;
        private MockHttpMessageHandler _mockHttp;
        private HttpClient _httpClient;
        
        [SetUp]
        public void Init()
        {
            _mockHttp = new MockHttpMessageHandler();
            _httpClient = new HttpClient(_mockHttp)
            {
                BaseAddress = new Uri("http://localhost")
            };
            
            _sut = new PokemonService(_httpClient);
        }
        
        [Test] 
        public void GetPokemonDataAsync_Throws_If_PokemonName_Is_Null()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _sut.GetPokemonDataAsync(null, default));
        }

        [Test]
        [TestCase("")]
        [TestCase(" ")]
        public void GetPokemonDataAsync_Throws_If_PokemonName_Is_Empty(string emptyString)
        {
            Assert.ThrowsAsync<ArgumentException>(() => _sut.GetPokemonDataAsync(emptyString, default));
        }

        [Test]
        public async Task
            GetPokemonDataAsync_Uses_Client_By_Sending_Get_Request_With_Pokemon_Name_As_Url_Parameter()
        {
            const string name = "mewtwo";

            var request = _mockHttp.When("/pokemon-species/mewtwo")
                .Respond(HttpStatusCode.NotFound);
            
            await _sut.GetPokemonDataAsync(name, default);

            Assert.AreEqual(1, _mockHttp.GetMatchCount(request));
        }

        [Test]
        public async Task GetPokemonDataAsync_Returns_Error_Response_If_Request_Was_Unsuccessful()
        {
            const string name = "mewtwo";

            const HttpStatusCode errorCode = HttpStatusCode.BadRequest;

            _mockHttp.When("/pokemon-species/*")
                .Respond(errorCode);
            
            var result = await _sut.GetPokemonDataAsync(name, default);
            
            Assert.IsFalse(result.Success);
            Assert.IsNull(result.Data);
            Assert.AreEqual(errorCode, result.StatusCode);
            Assert.AreEqual($"Could not get data from pokemon api, response indicates error: {errorCode}", result.Message);
        }

        [Test]
        public async Task
            GetPokemonDataAsync_Returns_Error_Response_If_Request_Was_Successful_But_Response_Content_Cannot_Be_Read()
        {
            const string name = "mewtwo";
            
            _mockHttp.When("/pokemon-species/*")
                .Respond("application/json", "not json data");

            var result = await _sut.GetPokemonDataAsync(name, default);

            Assert.IsFalse(result.Success);
            Assert.IsNull(result.Data);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.AreEqual("Response content was in an unexpected format", result.Message);
        }
        
        [Test]
        public async Task GetPokemonDataAsync_Returns_Success_Response_With_Pokemon_Info()
        {
            const string name = "mewtwo", hab = "hab", desc = "desc";
            const bool isLegendary = true;
            
            _mockHttp.When("/pokemon-species/*")
                .Respond("application/json", JsonSerializer.Serialize(new PokemonData
                {
                    Habitat = new PokemonData.HabitatData
                    {
                        Name = hab
                    },
                    IsLegendary = isLegendary,
                    Name = name,
                    FlavorTextEntries = new List<PokemonData.FlavorTextEntry>
                    {
                        new ()
                        {
                            Language = new PokemonData.Language { Name = "en" },
                            FlavorText = desc
                        }
                    }
                }));

            var result = await _sut.GetPokemonDataAsync(name, default);
            
            Assert.IsTrue(result.Success);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.IsNull(result.Message);
            Assert.AreEqual(name, result.Data.Name);
            Assert.AreEqual(desc, result.Data.Description);
            Assert.AreEqual(hab, result.Data.Habitat);
            Assert.AreEqual(isLegendary, result.Data.IsLegendary);
        }
        
        [Test]
        public async Task GetPokemonDataAsync_Returns_Habitat_As_Null_If_No_Habitat_Data()
        {
            const string name = "mewtwo", desc = "desc";
            const bool isLegendary = true;

            _mockHttp.When("/pokemon-species/*")
                .Respond("application/json", JsonSerializer.Serialize(new PokemonData
                    {
                        Habitat = null,
                        IsLegendary = isLegendary,
                        Name = name,
                        FlavorTextEntries = new List<PokemonData.FlavorTextEntry>
                        {
                            new ()
                            {
                                Language = new PokemonData.Language { Name = "en" },
                                FlavorText = desc
                            }
                        }
                    }
                ));
            
            var result = await _sut.GetPokemonDataAsync(name, default);
            
            Assert.IsTrue(result.Success);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.IsNull(result.Data.Habitat);
        }
        
        [Test]
        public async Task GetPokemonDataAsync_Returns_Description_As_Null_If_Flavor_Text_Entries_Null()
        {
            const string name = "mewtwo", hab = "hab";
            const bool isLegendary = true;

            _mockHttp.When("/pokemon-species/*")
                .Respond("application/json", JsonSerializer.Serialize(new PokemonData
                    {
                        Habitat = new PokemonData.HabitatData
                        {
                            Name = hab
                        },
                        IsLegendary = isLegendary,
                        Name = name,
                        FlavorTextEntries = null
                    }
                ));
            
            var result = await _sut.GetPokemonDataAsync(name, default);
            
            Assert.IsTrue(result.Success);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.IsNull(result.Data.Description);
        }
        
        [Test]
        public async Task GetPokemonDataAsync_Returns_Description_As_Null_If_Flavor_Text_Entries_Empty()
        {
            const string name = "mewtwo", hab = "hab";
            const bool isLegendary = true;

            _mockHttp.When("/pokemon-species/*")
                .Respond("application/json", JsonSerializer.Serialize(new PokemonData
                    {
                        Habitat = new PokemonData.HabitatData
                        {
                            Name = hab
                        },
                        IsLegendary = isLegendary,
                        Name = name,
                        FlavorTextEntries = new List<PokemonData.FlavorTextEntry>()
                    }
                ));
            
            var result = await _sut.GetPokemonDataAsync(name, default);
            
            Assert.IsTrue(result.Success);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.IsNull(result.Data.Description);
        }
        
        [Test]
        public async Task GetPokemonDataAsync_Returns_Description_As_Null_If_Flavor_Text_Entries_Has_No_English_Entries()
        {
            const string name = "mewtwo", hab = "hab";
            const bool isLegendary = true;

            _mockHttp.When("/pokemon-species/*")
                .Respond("application/json", JsonSerializer.Serialize(new PokemonData
                    {
                        Habitat = new PokemonData.HabitatData
                        {
                            Name = hab
                        },
                        IsLegendary = isLegendary,
                        Name = name,
                        FlavorTextEntries = new List<PokemonData.FlavorTextEntry>
                        {
                            new ()
                            {
                                Language = new PokemonData.Language
                                {
                                    Name = "fr"
                                },
                                FlavorText = "some french flavor text"
                            },
                            new ()
                            {
                                Language = null,
                                FlavorText = "some null lang flavor text"
                            }
                        }
                    }
                ));
            
            var result = await _sut.GetPokemonDataAsync(name, default);
            
            Assert.IsTrue(result.Success);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.IsNull(result.Data.Description);
        }
    }
}