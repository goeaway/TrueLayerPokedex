using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using TrueLayerPokedex.Domain.Models;
using TrueLayerPokedex.Infrastructure.Services.Translation;

namespace TrueLayerPokedex.Infrastructure.Tests.Services
{
    public class ShakespeareTranslatorTests
    {
        private ShakespeareTranslator _sut;
        private MockHttpMessageHandler _mockHttp;
        private HttpClient _httpClient;

        [SetUp]
        public void Init()
        {
            _mockHttp = new MockHttpMessageHandler();
            _httpClient = new HttpClient(_mockHttp) { BaseAddress = new Uri("http://localhost")};
            
            _sut = new ShakespeareTranslator(_httpClient);
        }

        [Test]
        public void CanTranslate_Returns_True()
        {
            var info = new PokemonInfo();

            var result = _sut.CanTranslate(info);
            
            Assert.IsTrue(result);
        }
        
        [Test]
        public void GetTranslationAsync_Throws_If_PokemonInfo_Null()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _sut.GetTranslationAsync(null, default));
        }

        [Test]
        public async Task GetTranslationAsync_Uses_Client_To_Get_Translation_For_PokemonInfo_Description()
        {
            const string description = "It is a description";
            
            var info = new PokemonInfo
            {
                Description = description
            };

            var request = _mockHttp.When("/shakespeare.json")
                .With(m =>
                {
                    var data = JsonSerializer.Deserialize<TranslationRequestData>(m.Content.ReadAsStringAsync()
                        .GetAwaiter().GetResult());    
                    return data.Text == description;
                })
                .Respond(HttpStatusCode.NotFound);

            await _sut.GetTranslationAsync(info, default);

            Assert.AreEqual(1, _mockHttp.GetMatchCount(request));
        }
        
        [Test]
        public async Task GetTranslationAsync_Returns_Same_PokemonInfo_As_Input_If_Request_Fails()
        {
            const string description = "It is a description";
            
            var info = new PokemonInfo
            {
                Description = description
            };

            _mockHttp.When("/shakespeare.json")
                .Respond(HttpStatusCode.NotFound);

            var result = await _sut.GetTranslationAsync(info, default);

            Assert.AreSame(info, result);
        }
        
        [Test]
        public async Task GetTranslationAsync_Returns_Same_PokemonInfo_As_Input_If_Request_Succeeds_But_Content_Could_Not_Be_Deserialised()
        {
            const string description = "It is a description";
            
            var info = new PokemonInfo
            {
                Description = description
            };

            _mockHttp.When("/shakespeare.json")
                .Respond("application/json", "not json");

            var result = await _sut.GetTranslationAsync(info, default);

            Assert.AreSame(info, result);
        }
        
        [Test]
        public async Task GetTranslationAsync_Returns_New_PokemonInfo_Using_Translated_Text_If_Request_Succeeds_And_Can_Be_Deserialised()
        {
            const string name = "name", habitat = "hab";
            const bool isLegendary = true;
            const string inputDescription = "It is a description", translatedDescription = "translated description";
            
            var info = new PokemonInfo
            {
                Name = name,
                Habitat = habitat,
                IsLegendary = isLegendary,
                Description = inputDescription
            };

            _mockHttp.When("/shakespeare.json")
                .Respond("application/json", JsonSerializer.Serialize(new TranslationResponseData
                {
                    Success = new TranslationResponseData.SuccessData
                    {
                        Total = 1
                    },
                    Contents = new TranslationResponseData.ContentsData
                    {
                        Translated = translatedDescription,
                        Text = inputDescription,
                        Translation = "yoda"
                    }
                }));

            var result = await _sut.GetTranslationAsync(info, default);

            Assert.AreNotSame(info, result);
            Assert.AreEqual(name, result.Name);
            Assert.AreEqual(translatedDescription, result.Description);
            Assert.AreEqual(habitat, result.Habitat);
            Assert.AreEqual(isLegendary, result.IsLegendary);
        }
        
        [Test]
        public async Task GetTranslationAsync_Returns_Input_PokemonInfo_If_Request_Succeeded_And_Could_Be_Deserialised_But_Contents_Was_Null()
        {
            const string name = "name", habitat = "hab";
            const bool isLegendary = true;
            const string inputDescription = "It is a description";
            
            var info = new PokemonInfo
            {
                Name = name,
                Habitat = habitat,
                IsLegendary = isLegendary,
                Description = inputDescription
            };

            _mockHttp.When("/shakespeare.json")
                .Respond("application/json", JsonSerializer.Serialize(new TranslationResponseData
                {
                    Success = new TranslationResponseData.SuccessData
                    {
                        Total = 1
                    },
                    Contents = null
                }));

            var result = await _sut.GetTranslationAsync(info, default);

            Assert.AreSame(info, result);
        }
        
        [Test]
        public async Task GetTranslationAsync_Returns_Input_PokemonInfo_If_Request_Succeeded_And_Could_Be_Deserialised_Contents_Was_Not_Null_But_Translated_Was_Null()
        {
            const string name = "name", habitat = "hab";
            const bool isLegendary = true;
            const string inputDescription = "It is a description";
            
            var info = new PokemonInfo
            {
                Name = name,
                Habitat = habitat,
                IsLegendary = isLegendary,
                Description = inputDescription
            };

            _mockHttp.When("/shakespeare.json")
                .Respond("application/json", JsonSerializer.Serialize(new TranslationResponseData
                {
                    Success = new TranslationResponseData.SuccessData
                    {
                        Total = 1
                    },
                    Contents = new TranslationResponseData.ContentsData
                    {
                        Translated = null,
                        Text = inputDescription,
                        Translation = "yoda"
                    }
                }));

            var result = await _sut.GetTranslationAsync(info, default);

            Assert.AreSame(info, result);
        }
        
        [Test]
        public async Task GetTranslationAsync_Returns_Input_PokemonInfo_If_Request_Succeeded_And_Could_Be_Deserialised_Contents_Was_Not_Null_But_Translated_Was_Empty()
        {
            const string name = "name", habitat = "hab";
            const bool isLegendary = true;
            const string inputDescription = "It is a description";
            
            var info = new PokemonInfo
            {
                Name = name,
                Habitat = habitat,
                IsLegendary = isLegendary,
                Description = inputDescription
            };

            _mockHttp.When("/shakespeare.json")
                .Respond("application/json", JsonSerializer.Serialize(new TranslationResponseData
                {
                    Success = new TranslationResponseData.SuccessData
                    {
                        Total = 1
                    },
                    Contents = new TranslationResponseData.ContentsData
                    {
                        Translated = "",
                        Text = inputDescription,
                        Translation = "yoda"
                    }
                }));

            var result = await _sut.GetTranslationAsync(info, default);

            Assert.AreSame(info, result);
        }
    }
}