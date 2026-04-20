using System.Collections.Generic;
using PokeRed.Items;
using PokeRed.Pokemon;
using UnityEngine;

namespace PokeRed.Save
{
    [CreateAssetMenu(fileName = "PokemonDataRegistry", menuName = "PokeRed/Data Registry")]
    public class PokemonDataRegistry : ScriptableObject
    {
        public List<PokemonData> species = new();
        public List<MoveData>    moves   = new();
        public List<ItemData>    items   = new();

        private Dictionary<string, PokemonData> speciesByName;
        private Dictionary<string, MoveData>    movesByName;
        private Dictionary<string, ItemData>    itemsByName;

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

        public ItemData FindItem(string assetName)
        {
            BuildCache();
            return itemsByName.TryGetValue(assetName ?? "", out var i) ? i : null;
        }

        private void BuildCache()
        {
            if (speciesByName != null && movesByName != null && itemsByName != null) return;
            speciesByName = new Dictionary<string, PokemonData>();
            movesByName   = new Dictionary<string, MoveData>();
            itemsByName   = new Dictionary<string, ItemData>();
            foreach (var s in species) if (s != null) speciesByName[s.name] = s;
            foreach (var m in moves)   if (m != null) movesByName[m.name]   = m;
            foreach (var i in items)   if (i != null) itemsByName[i.name]   = i;
        }
    }
}
