using System.Collections.Generic;
using PokeRed.Pokemon;
using UnityEngine;

namespace PokeRed.Save
{
    [CreateAssetMenu(fileName = "PokemonDataRegistry", menuName = "PokeRed/Data Registry")]
    public class PokemonDataRegistry : ScriptableObject
    {
        public List<PokemonData> species = new();
        public List<MoveData>    moves   = new();

        private Dictionary<string, PokemonData> speciesByName;
        private Dictionary<string, MoveData>    movesByName;

        public PokemonData FindSpecies(string assetName)
        {
            BuildCache();
            return speciesByName.TryGetValue(assetName ?? "", out var s) ? s : null;
        }

        public MoveData FindMove(string assetName)
        {
            BuildCache();
            return movesByName.TryGetValue(assetName ?? "", out var m) ? m : null;
        }

        private void BuildCache()
        {
            if (speciesByName != null && movesByName != null) return;
            speciesByName = new Dictionary<string, PokemonData>();
            movesByName   = new Dictionary<string, MoveData>();
            foreach (var s in species) if (s != null) speciesByName[s.name] = s;
            foreach (var m in moves)   if (m != null) movesByName[m.name]   = m;
        }
    }
}
