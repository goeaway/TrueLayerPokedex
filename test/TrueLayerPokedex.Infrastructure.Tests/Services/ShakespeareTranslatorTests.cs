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
        public async Task GetTranslationAsync_Returns_Null_If_Description_Null()
        {
            var result = await _sut.GetTranslationAsync(null, default);
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetTranslationAsync_Uses_Client_To_Get_Translation_For_PokemonInfo_Description()
        {
            const string description = "It is a description";
            
            var request = _mockHttp.When("/shakespeare.json")
                .With(m =>
                {
                    var data = JsonSerializer.Deserialize<TranslationRequestData>(m.Content.ReadAsStringAsync()
                        .GetAwaiter().GetResult());    
                    return data?.Text == description;
                })
                .Respond(HttpStatusCode.NotFound);

            await _sut.GetTranslationAsync(description, default);

            Assert.AreEqual(1, _mockHttp.GetMatchCount(request));
        }
        
        [Test]
        public async Task GetTranslationAsync_Returns_Same_Description_As_Input_If_Request_Fails()
        {
            const string description = "It is a description";
            
            _mockHttp.When("/shakespeare.json")
                .Respond(HttpStatusCode.NotFound);

            var result = await _sut.GetTranslationAsync(description, default);

            Assert.AreEqual(description, result);
        }
        
        [Test]
        public async Task GetTranslationAsync_Returns_Same_Description_As_Input_If_Request_Succeeds_But_Content_Could_Not_Be_Deserialised()
        {
            const string description = "It is a description";
            
            _mockHttp.When("/shakespeare.json")
                .Respond("application/json", "not json");

            var result = await _sut.GetTranslationAsync(description, default);

            Assert.AreEqual(description, result);
        }
        
        [Test]
        public async Task GetTranslationAsync_Returns_New_Description_Using_Translated_Text_If_Request_Succeeds_And_Can_Be_Deserialised()
        {
            const string inputDescription = "It is a description", translatedDescription = "translated description";
            
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

            var result = await _sut.GetTranslationAsync(inputDescription, default);

            Assert.AreEqual(translatedDescription, result);
        }
        
        [Test]
        public async Task GetTranslationAsync_Returns_Input_Description_If_Request_Succeeded_And_Could_Be_Deserialised_But_Contents_Was_Null()
        {
            const string inputDescription = "It is a description";
            
            _mockHttp.When("/shakespeare.json")
                .Respond("application/json", JsonSerializer.Serialize(new TranslationResponseData
                {
                    Success = new TranslationResponseData.SuccessData
                    {
                        Total = 1
                    },
                    Contents = null
                }));

            var result = await _sut.GetTranslationAsync(inputDescription, default);

            Assert.AreEqual(inputDescription, result);
        }
        
        [Test]
        public async Task GetTranslationAsync_Returns_Input_Description_If_Request_Succeeded_And_Could_Be_Deserialised_Contents_Was_Not_Null_But_Translated_Was_Null()
        {
            const string inputDescription = "It is a description";
            
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

            var result = await _sut.GetTranslationAsync(inputDescription, default);

            Assert.AreEqual(inputDescription, result);
        }
        
        [Test]
        public async Task GetTranslationAsync_Returns_Input_Description_If_Request_Succeeded_And_Could_Be_Deserialised_Contents_Was_Not_Null_But_Translated_Was_Empty()
        {
            const string inputDescription = "It is a description";
            
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

            var result = await _sut.GetTranslationAsync(inputDescription, default);

            Assert.AreEqual(inputDescription, result);
        }
    }
}