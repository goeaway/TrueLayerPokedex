using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TrueLayerPokedex.Application.Queries.GetBasicPokemonInfo;
using TrueLayerPokedex.Application.Queries.GetTranslatedPokemonInfo;

namespace TrueLayerPokedex.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PokemonController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PokemonController(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        [HttpGet("translated/{pokemonName}")]
        public async Task<IActionResult> GetTranslatedInfo(string pokemonName, CancellationToken cancellationToken)
        {
            var result = await _mediator
                .Send(new GetTranslatedPokemonInfoQuery
                {
                    PokemonName = pokemonName
                }, cancellationToken);

            return result.Match(Ok, error => StatusCode((int) error.StatusCode, error));
        }

        [HttpGet("{pokemonName}")]
        public async Task<IActionResult> GetBasicInfo(string pokemonName, CancellationToken cancellationToken)
        {
            var result = await _mediator
                .Send(new GetBasicPokemonInfoQuery
                {
                    PokemonName = pokemonName
                }, cancellationToken);

            return result.Match(
                    Ok, 
                    error => StatusCode((int) error.StatusCode, error));
        }

    }
}