using System;
using System.Collections.Generic;
using UnityEngine;

namespace PokeRed.Pokemon
{
    [Serializable]
    public struct LevelMove
    {
        public int level;
        public MoveData move;
    }

    [CreateAssetMenu(fileName = "PokemonData", menuName = "PokeRed/Pokemon Species")]
    public class PokemonData : ScriptableObject
    {
        [Header("Identity")]
        public string speciesName = "Placeholder";
        public int    dexNumber   = 0;

        [Header("Types")]
        public PokemonType type1 = PokemonType.Normal;
        public PokemonType type2 = PokemonType.None;

        [Header("Base Stats")]
        public int baseHP     = 50;
        public int baseAtk    = 50;
        public int baseDef    = 50;
        public int baseSpAtk  = 50;
        public int baseSpDef  = 50;
        public int baseSpeed  = 50;

        [Header("Growth")]
        [Tooltip("Total XP to reach level 100. Classic values: 800k (fast), 1M (medium-fast), 1.25M (medium-slow), 1.64M (slow).")]
        public int maxExp = 1000000;
        public int catchRate = 45;
        public int baseExpYield = 64;

        [Header("Moves")]
        public List<LevelMove> learnset = new();

        [Header("Art")]
        public Sprite frontSprite;
        public Sprite backSprite;
        public AudioClip cry;

        public int ExpForLevel(int level)
        {
            // Medium-Fast curve: n^3
            return Mathf.RoundToInt(Mathf.Pow(level, 3));
        }
    }
}
