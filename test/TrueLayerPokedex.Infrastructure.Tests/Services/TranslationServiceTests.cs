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
        public async Task GetTranslationAsync_Returns_PokemonInfo_From_Translator_If_One_Is_Found_To_Translate()
        {
            var input = new PokemonInfo();
            var translated = new PokemonInfo();

            var translator = new Mock<ITranslator>();
            
            var sut = new TranslationService(new List<ITranslator> { translator.Object });

            translator.Setup(mock => mock.CanTranslate(input)).Returns(true);
            translator.Setup(mock => mock.GetTranslationAsync(input, It.IsAny<CancellationToken>())).ReturnsAsync(translated);

            var result = await sut.GetTranslationAsync(input, It.IsAny<CancellationToken>());
            
            Assert.AreNotSame(input, result);
            Assert.AreSame(translated, result);
        }
        
        [Test]
        public async Task GetTranslationAsync_Returns_PokemonInfo_From_First_Translator_If_Multiple_Can_Translate()
        {
            var input = new PokemonInfo();
            var translated1 = new PokemonInfo();
            var translated2 = new PokemonInfo();

            var translator1 = new Mock<ITranslator>();
            var translator2 = new Mock<ITranslator>();
            
            var sut = new TranslationService(new List<ITranslator> { translator1.Object, translator2.Object });

            translator1.Setup(mock => mock.CanTranslate(input)).Returns(true);
            translator1.Setup(mock => mock.GetTranslationAsync(input, It.IsAny<CancellationToken>())).ReturnsAsync(translated1);
            
            translator2.Setup(mock => mock.CanTranslate(input)).Returns(true);
            translator2.Setup(mock => mock.GetTranslationAsync(input, It.IsAny<CancellationToken>())).ReturnsAsync(translated2);

            var result = await sut.GetTranslationAsync(input, default);
            
            Assert.AreSame(translated1, result);
        }
    }
}