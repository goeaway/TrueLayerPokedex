namespace TrueLayerPokedex.Domain.Dtos
{
    public class PokemonInfoDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Habitat { get; set; }
        public bool IsLegendary { get; set; }
    }
}