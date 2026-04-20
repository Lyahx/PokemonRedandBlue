using PokeRed.Battle;
using PokeRed.Core;
using PokeRed.Pokemon;
using UnityEngine;

namespace PokeRed.DebugTools
{
    /// <summary>Editor helper: press B to spawn a test wild battle against a placeholder.</summary>
    public class BattleTrigger : MonoBehaviour
    {
        [SerializeField] private KeyCode triggerKey = KeyCode.B;
        [SerializeField] private int enemyLevel = 6;

        private void Update()
        {
            if (!Input.GetKeyDown(triggerKey)) return;
            if (GameManager.Instance == null || BattleSystem.Instance == null) return;
            if (GameManager.Instance.State != GameState.Overworld) return;
            if (!GameManager.Instance.PlayerParty.HasUsable) return;

            var move   = ScriptableObject.CreateInstance<MoveData>();
            move.name  = "Tackle"; move.moveName = "Tackle";
            move.type  = PokemonType.Normal; move.category = MoveCategory.Physical;
            move.power = 40; move.accuracy = 100; move.pp = 35;

            var species = ScriptableObject.CreateInstance<PokemonData>();
            species.name = "WildDummy"; species.speciesName = "WildDummy";
            species.type1 = PokemonType.Grass; species.type2 = PokemonType.None;
            species.baseHP = 45; species.baseAtk = 49; species.baseDef = 49;
            species.baseSpAtk = 65; species.baseSpDef = 65; species.baseSpeed = 45;
            species.baseExpYield = 64; species.catchRate = 45;
            species.learnset.Add(new LevelMove { level = 1, move = move });

            var wild = PokemonInstance.Create(species, enemyLevel);
            BattleSystem.Instance.StartWildBattle(GameManager.Instance.PlayerParty, wild);
        }
    }
}
