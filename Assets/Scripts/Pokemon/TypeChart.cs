using System.Collections.Generic;

namespace PokeRed.Pokemon
{
    // Gen 1 effectiveness multipliers. Missing entries default to 1.0x.
    public static class TypeChart
    {
        private static readonly Dictionary<(PokemonType atk, PokemonType def), float> table = new()
        {
            // Normal
            {(PokemonType.Normal, PokemonType.Rock), 0.5f}, {(PokemonType.Normal, PokemonType.Ghost), 0f},
            // Fire
            {(PokemonType.Fire, PokemonType.Fire), 0.5f}, {(PokemonType.Fire, PokemonType.Water), 0.5f},
            {(PokemonType.Fire, PokemonType.Grass), 2f},  {(PokemonType.Fire, PokemonType.Ice), 2f},
            {(PokemonType.Fire, PokemonType.Bug), 2f},    {(PokemonType.Fire, PokemonType.Rock), 0.5f},
            {(PokemonType.Fire, PokemonType.Dragon), 0.5f},
            // Water
            {(PokemonType.Water, PokemonType.Fire), 2f},  {(PokemonType.Water, PokemonType.Water), 0.5f},
            {(PokemonType.Water, PokemonType.Grass), 0.5f}, {(PokemonType.Water, PokemonType.Ground), 2f},
            {(PokemonType.Water, PokemonType.Rock), 2f},  {(PokemonType.Water, PokemonType.Dragon), 0.5f},
            // Electric
            {(PokemonType.Electric, PokemonType.Water), 2f},   {(PokemonType.Electric, PokemonType.Electric), 0.5f},
            {(PokemonType.Electric, PokemonType.Grass), 0.5f}, {(PokemonType.Electric, PokemonType.Ground), 0f},
            {(PokemonType.Electric, PokemonType.Flying), 2f},  {(PokemonType.Electric, PokemonType.Dragon), 0.5f},
            // Grass
            {(PokemonType.Grass, PokemonType.Fire), 0.5f},  {(PokemonType.Grass, PokemonType.Water), 2f},
            {(PokemonType.Grass, PokemonType.Grass), 0.5f}, {(PokemonType.Grass, PokemonType.Poison), 0.5f},
            {(PokemonType.Grass, PokemonType.Ground), 2f},  {(PokemonType.Grass, PokemonType.Flying), 0.5f},
            {(PokemonType.Grass, PokemonType.Bug), 0.5f},   {(PokemonType.Grass, PokemonType.Rock), 2f},
            {(PokemonType.Grass, PokemonType.Dragon), 0.5f},
            // Ice
            {(PokemonType.Ice, PokemonType.Water), 0.5f},  {(PokemonType.Ice, PokemonType.Grass), 2f},
            {(PokemonType.Ice, PokemonType.Ice), 0.5f},    {(PokemonType.Ice, PokemonType.Ground), 2f},
            {(PokemonType.Ice, PokemonType.Flying), 2f},   {(PokemonType.Ice, PokemonType.Dragon), 2f},
            // Fighting
            {(PokemonType.Fighting, PokemonType.Normal), 2f},   {(PokemonType.Fighting, PokemonType.Ice), 2f},
            {(PokemonType.Fighting, PokemonType.Poison), 0.5f}, {(PokemonType.Fighting, PokemonType.Flying), 0.5f},
            {(PokemonType.Fighting, PokemonType.Psychic), 0.5f},{(PokemonType.Fighting, PokemonType.Bug), 0.5f},
            {(PokemonType.Fighting, PokemonType.Rock), 2f},     {(PokemonType.Fighting, PokemonType.Ghost), 0f},
            // Poison
            {(PokemonType.Poison, PokemonType.Grass), 2f},   {(PokemonType.Poison, PokemonType.Poison), 0.5f},
            {(PokemonType.Poison, PokemonType.Ground), 0.5f},{(PokemonType.Poison, PokemonType.Bug), 2f},
            {(PokemonType.Poison, PokemonType.Rock), 0.5f},  {(PokemonType.Poison, PokemonType.Ghost), 0.5f},
            // Ground
            {(PokemonType.Ground, PokemonType.Fire), 2f},   {(PokemonType.Ground, PokemonType.Electric), 2f},
            {(PokemonType.Ground, PokemonType.Grass), 0.5f},{(PokemonType.Ground, PokemonType.Poison), 2f},
            {(PokemonType.Ground, PokemonType.Flying), 0f}, {(PokemonType.Ground, PokemonType.Bug), 0.5f},
            {(PokemonType.Ground, PokemonType.Rock), 2f},
            // Flying
            {(PokemonType.Flying, PokemonType.Electric), 0.5f},{(PokemonType.Flying, PokemonType.Grass), 2f},
            {(PokemonType.Flying, PokemonType.Fighting), 2f},  {(PokemonType.Flying, PokemonType.Bug), 2f},
            {(PokemonType.Flying, PokemonType.Rock), 0.5f},
            // Psychic
            {(PokemonType.Psychic, PokemonType.Fighting), 2f}, {(PokemonType.Psychic, PokemonType.Poison), 2f},
            {(PokemonType.Psychic, PokemonType.Psychic), 0.5f},
            // Bug
            {(PokemonType.Bug, PokemonType.Fire), 0.5f},    {(PokemonType.Bug, PokemonType.Grass), 2f},
            {(PokemonType.Bug, PokemonType.Fighting), 0.5f},{(PokemonType.Bug, PokemonType.Poison), 2f},
            {(PokemonType.Bug, PokemonType.Flying), 0.5f},  {(PokemonType.Bug, PokemonType.Psychic), 2f},
            {(PokemonType.Bug, PokemonType.Ghost), 0.5f},
            // Rock
            {(PokemonType.Rock, PokemonType.Fire), 2f},    {(PokemonType.Rock, PokemonType.Ice), 2f},
            {(PokemonType.Rock, PokemonType.Fighting), 0.5f},{(PokemonType.Rock, PokemonType.Ground), 0.5f},
            {(PokemonType.Rock, PokemonType.Flying), 2f},  {(PokemonType.Rock, PokemonType.Bug), 2f},
            // Ghost
            {(PokemonType.Ghost, PokemonType.Normal), 0f}, {(PokemonType.Ghost, PokemonType.Psychic), 0f},
            {(PokemonType.Ghost, PokemonType.Ghost), 2f},
            // Dragon
            {(PokemonType.Dragon, PokemonType.Dragon), 2f},
        };

        public static float Effectiveness(PokemonType attacking, PokemonType defending)
        {
            if (attacking == PokemonType.None || defending == PokemonType.None) return 1f;
            return table.TryGetValue((attacking, defending), out var v) ? v : 1f;
        }

        public static float Effectiveness(PokemonType attacking, PokemonType def1, PokemonType def2)
        {
            return Effectiveness(attacking, def1) * Effectiveness(attacking, def2);
        }
    }
}
