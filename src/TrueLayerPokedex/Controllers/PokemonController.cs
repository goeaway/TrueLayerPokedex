using Microsoft.AspNetCore.Mvc;

namespace TrueLayerPokedex.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PokemonController : ControllerBase 
    {
        public IActionResult Test()
        {
            return Ok("success");
        }
    }
}