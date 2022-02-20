using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TrueLayerPokedex.Application.Queries.GetBasicPokemonInfo;

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