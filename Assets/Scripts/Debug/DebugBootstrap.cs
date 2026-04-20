using PokeRed.Core;
using PokeRed.Items;
using PokeRed.Pokemon;
using PokeRed.Save;
using UnityEngine;

namespace PokeRed.DebugTools
{
    /// <summary>
    /// Editor/playtest helper: places a starter Pokemon into the player's party
    /// and stocks the bag with a few test items so battles + catching can be
    /// exercised without a full intro flow. Prefers assets from the
    /// PokemonDataRegistry if one is assigned, otherwise falls back to
    /// runtime-generated placeholders.
    /// </summary>
    public class DebugBootstrap : MonoBehaviour
    {
        [SerializeField] private bool addTestParty = true;
        [SerializeField] private int  startingLevel = 7;
        [SerializeField] private PokemonDataRegistry registry;

        private void Start()
        {
            if (GameManager.Instance == null || !addTestParty) return;
            if (GameManager.Instance.PlayerParty.Count > 0) return;

            if (registry == null) registry = Resources.Load<PokemonDataRegistry>("PokemonDataRegistry");
            if (registry != null && registry.species.Count > 0)
            {
                SeedFromRegistry();
                return;
            }

            var tackle  = CreateMove("Tackle",  PokemonType.Normal,   MoveCategory.Physical, 40, 100, 35);
            var ember   = CreateMove("Ember",   PokemonType.Fire,     MoveCategory.Special,  40, 100, 25);
            var growl   = CreateMove("Growl",   PokemonType.Normal,   MoveCategory.Status,    0, 100, 40);
            var spark   = CreateMove("Spark",   PokemonType.Electric, MoveCategory.Special,  65, 100, 20);

            var species = CreateSpecies("Flamekin", PokemonType.Fire, PokemonType.None,
                hp: 39, atk: 52, def: 43, spAtk: 60, spDef: 50, speed: 65);
            species.learnset.Add(new LevelMove { level = 1, move = tackle });
            species.learnset.Add(new LevelMove { level = 1, move = growl });
            species.learnset.Add(new LevelMove { level = 5, move = ember });
            species.learnset.Add(new LevelMove { level = 9, move = spark });

            var mon = PokemonInstance.Create(species, startingLevel);
            mon.nickname = "Test";
            GameManager.Instance.PlayerParty.Add(mon);
        }

        private void SeedFromRegistry()
        {
            var species = registry.species[0];
            var mon = PokemonInstance.Create(species, startingLevel);
            GameManager.Instance.PlayerParty.Add(mon);

            // Give the player a couple of test items if the registry has them.
            foreach (var item in registry.items)
            {
                if (item == null) continue;
                if (item.category == ItemCategory.Ball)   GameManager.Instance.Bag.Add(item, 5);
                if (item.category == ItemCategory.Potion) GameManager.Instance.Bag.Add(item, 3);
            }
        }

        private static MoveData CreateMove(string name, PokemonType type, MoveCategory cat, int power, int acc, int pp)
        {
            var m = ScriptableObject.CreateInstance<MoveData>();
            m.name = name; m.moveName = name; m.type = type; m.category = cat;
            m.power = power; m.accuracy = acc; m.pp = pp;
            return m;
        }

        private static PokemonData CreateSpecies(string name, PokemonType t1, PokemonType t2,
                                                 int hp, int atk, int def, int spAtk, int spDef, int speed)
        {
            var s = ScriptableObject.CreateInstance<PokemonData>();
            s.name = name; s.speciesName = name;
            s.type1 = t1; s.type2 = t2;
            s.baseHP = hp; s.baseAtk = atk; s.baseDef = def;
            s.baseSpAtk = spAtk; s.baseSpDef = spDef; s.baseSpeed = speed;
            s.baseExpYield = 64; s.catchRate = 45;
            return s;
        }
    }
}
