using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using TrueLayerPokedex.Domain.Models;
using TrueLayerPokedex.Infrastructure.Services.Translation;

namespace TrueLayerPokedex.Infrastructure.Tests.Services
{
    public class TranslationServiceTests
    {
        [Test]
        public async Task GetTranslationAsync_Throws_If_PokemonInfo_Is_Null()
        {
            var sut = new TranslationService(new List<ITranslator>());
            Assert.ThrowsAsync<ArgumentNullException>(() => sut.GetTranslationAsync(null, default));
        }

        [Test]
        public async Task GetTranslationAsync_Returns_Same_PokemonInfo_If_No_Translators_Provided()
        {
            var input = new PokemonInfo();

            var sut = new TranslationService(new List<ITranslator>());

            var result = await sut.GetTranslationAsync(input, default);
            
            Assert.AreSame(input, result);
        }

        [Test]
        public async Task GetTranslationAsync_Returns_Same_PokemonInfo_If_No_Translators_Could_Translate()
        {
            var input = new PokemonInfo();

            var translator = new Mock<ITranslator>();
            
            var sut = new TranslationService(new List<ITranslator> { translator.Object });

            translator.Setup(mock => mock.CanTranslate(input)).Returns(false);

            var result = await sut.GetTranslationAsync(input, default);
            
            Assert.AreSame(input, result);
        }

        [Test]
        public async Task GetTranslationAsync_Returns_PokemonInfo_With_Description_From_Translator_If_One_Is_Found_To_Translate()
        {
            const string name = "name", description = "description", habitat = "hab";
            const bool isLegendary = true;
            
            var input = new PokemonInfo
            {
                Name = name,
                Description = description,
                Habitat = habitat,
                IsLegendary = isLegendary
            };
            
            const string translated = "translated";

            var translator = new Mock<ITranslator>();
            
            var sut = new TranslationService(new List<ITranslator> { translator.Object });

            translator.Setup(mock => mock.CanTranslate(input)).Returns(true);
            translator.Setup(mock => mock.GetTranslationAsync(description, It.IsAny<CancellationToken>())).ReturnsAsync(translated);

            var result = await sut.GetTranslationAsync(input, It.IsAny<CancellationToken>());
            
            Assert.AreEqual(translated, result.Description);
            Assert.AreEqual(name, result.Name);
            Assert.AreEqual(habitat, result.Habitat);
            Assert.AreEqual(isLegendary, result.IsLegendary);
        }
        
        [Test]
        public async Task GetTranslationAsync_Returns_PokemonInfo_With_Translation_From_First_Translator_If_Multiple_Can_Translate()
        {
            const string name = "name", description = "description", habitat = "hab";
            const bool isLegendary = true;
            
            var input = new PokemonInfo
            {
                Name = name,
                Description = description,
                Habitat = habitat,
                IsLegendary = isLegendary
            };
            const string translated1 = "translated 1";
            const string translated2 = "translated 2";

            var translator1 = new Mock<ITranslator>();
            var translator2 = new Mock<ITranslator>();
            
            var sut = new TranslationService(new List<ITranslator> { translator1.Object, translator2.Object });

            translator1.Setup(mock => mock.CanTranslate(input)).Returns(true);
            translator1.Setup(mock => mock.GetTranslationAsync(input.Description, It.IsAny<CancellationToken>())).ReturnsAsync(translated1);
            
            translator2.Setup(mock => mock.CanTranslate(input)).Returns(true);
            translator2.Setup(mock => mock.GetTranslationAsync(input.Description, It.IsAny<CancellationToken>())).ReturnsAsync(translated2);

            var result = await sut.GetTranslationAsync(input, default);
            
            Assert.AreNotSame(input, result);
            Assert.AreEqual(translated1, result.Description);
            Assert.AreEqual(name, result.Name);
            Assert.AreEqual(habitat, result.Habitat);
            Assert.AreEqual(isLegendary, result.IsLegendary);
        }
    }
}