using System;
using System.Collections.Generic;
using PokeRed.Pokemon;
using UnityEngine;

namespace PokeRed.Battle
{
    [Serializable]
    public struct TrainerPokemon
    {
        public PokemonData species;
        public int level;
    }

    [CreateAssetMenu(fileName = "TrainerData", menuName = "PokeRed/Trainer")]
    public class TrainerData : ScriptableObject
    {
        public string trainerName = "Youngster Joey";
        public int prizeMoneyPerLevel = 12;
        public Sprite sprite;

        [Header("Pre/post battle lines")]
        [TextArea] public List<string> preBattleLines  = new();
        [TextArea] public List<string> lossLines       = new();
        [TextArea] public List<string> defeatedByLines = new();

        [Header("Party")]
        public List<TrainerPokemon> party = new();

        public List<PokemonInstance> BuildParty()
        {
            var list = new List<PokemonInstance>();
            foreach (var tp in party)
            {
                if (tp.species == null) continue;
                list.Add(PokemonInstance.Create(tp.species, tp.level));
            }
            return list;
        }

        public int RewardMoney()
        {
            int maxLvl = 0;
            foreach (var tp in party) if (tp.level > maxLvl) maxLvl = tp.level;
            return prizeMoneyPerLevel * Mathf.Max(1, maxLvl);
        }
    }
}
