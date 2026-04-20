using System;
using System.Collections.Generic;
using PokeRed.Pokemon;
using UnityEngine;

namespace PokeRed.World
{
    [Serializable]
    public struct EncounterSlot
    {
        public PokemonData species;
        public int minLevel;
        public int maxLevel;
        [Tooltip("Relative weight for random selection.")]
        public int weight;
    }

    [CreateAssetMenu(fileName = "EncounterTable", menuName = "PokeRed/Encounter Table")]
    public class EncounterTable : ScriptableObject
    {
        [Tooltip("Chance per step of triggering an encounter (0–1).")]
        [Range(0f, 1f)] public float stepEncounterChance = 0.1f;

        public List<EncounterSlot> slots = new();

        public PokemonInstance RollEncounter()
        {
            if (slots.Count == 0) return null;
            int totalWeight = 0;
            foreach (var s in slots) totalWeight += Mathf.Max(0, s.weight);
            if (totalWeight <= 0) return null;

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int acc = 0;
            foreach (var s in slots)
            {
                acc += Mathf.Max(0, s.weight);
                if (roll < acc)
                {
                    int level = UnityEngine.Random.Range(s.minLevel, s.maxLevel + 1);
                    return PokemonInstance.Create(s.species, level);
                }
            }
            return null;
        }
    }
}
