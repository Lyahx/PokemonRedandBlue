#if UNITY_EDITOR
using System.IO;
using PokeRed.Battle;
using PokeRed.Items;
using PokeRed.Pokemon;
using PokeRed.Save;
using PokeRed.World;
using UnityEditor;
using UnityEngine;

namespace PokeRed.EditorTools
{
    /// <summary>
    /// Generates a minimal set of ScriptableObject content — moves, species, items,
    /// encounter tables, and a data registry — so battles and encounters work with
    /// real assets instead of runtime-only placeholders. All names are deliberately
    /// generic placeholders; replace with your own Pokémon data as you go.
    /// Run via <c>PokeRed/Setup/Generate Sample Data</c>.
    /// </summary>
    public static class SampleDataBuilder
    {
        private const string DataDir          = "Assets/Data";
        private const string MovesDir         = "Assets/Data/Moves";
        private const string SpeciesDir       = "Assets/Data/Species";
        private const string ItemsDir         = "Assets/Data/Items";
        private const string EncountersDir    = "Assets/Data/Encounters";
        private const string TrainersDir      = "Assets/Data/Trainers";
        private const string RegistryPath     = "Assets/Data/PokemonDataRegistry.asset";

        [MenuItem("PokeRed/Setup/Generate Sample Data")]
        public static void Generate()
        {
            EnsureFolders();

            // ----- Moves -----
            var tackle      = CreateMove("Tackle",       PokemonType.Normal,   MoveCategory.Physical, 40, 100, 35, 0);
            var scratch     = CreateMove("Scratch",      PokemonType.Normal,   MoveCategory.Physical, 40, 100, 35, 0);
            var ember       = CreateMove("Ember",        PokemonType.Fire,     MoveCategory.Special,  40, 100, 25, 0);
            var waterGun    = CreateMove("Water Gun",    PokemonType.Water,    MoveCategory.Special,  40, 100, 25, 0);
            var vineWhip    = CreateMove("Vine Whip",    PokemonType.Grass,    MoveCategory.Special,  45, 100, 25, 0);
            var quickAttack = CreateMove("Quick Attack", PokemonType.Normal,   MoveCategory.Physical, 40, 100, 30, 1);
            var growl       = CreateMove("Growl",        PokemonType.Normal,   MoveCategory.Status,    0, 100, 40, 0);
            var thunderShock= CreateMove("Thunder Shock",PokemonType.Electric, MoveCategory.Special,  40, 100, 30, 0);

            // ----- Species (placeholder names, Gen1-inspired stat spreads) -----
            var sproutling = CreateSpecies(
                "Sproutling", 1, PokemonType.Grass, PokemonType.None,
                hp: 45, atk: 49, def: 49, spAtk: 65, spDef: 65, speed: 45,
                catchRate: 45, expYield: 64,
                learnset: new[] { (1, tackle), (1, growl), (7, vineWhip) });

            var kindling = CreateSpecies(
                "Kindling", 4, PokemonType.Fire, PokemonType.None,
                hp: 39, atk: 52, def: 43, spAtk: 60, spDef: 50, speed: 65,
                catchRate: 45, expYield: 64,
                learnset: new[] { (1, scratch), (1, growl), (7, ember) });

            var droplet = CreateSpecies(
                "Droplet", 7, PokemonType.Water, PokemonType.None,
                hp: 44, atk: 48, def: 65, spAtk: 50, spDef: 64, speed: 43,
                catchRate: 45, expYield: 64,
                learnset: new[] { (1, tackle), (1, growl), (7, waterGun) });

            var meadowhopper = CreateSpecies(
                "Meadowhopper", 10, PokemonType.Normal, PokemonType.None,
                hp: 40, atk: 55, def: 40, spAtk: 20, spDef: 35, speed: 90,
                catchRate: 255, expYield: 56,
                learnset: new[] { (1, tackle), (1, quickAttack) });

            var sparkmouse = CreateSpecies(
                "Sparkmouse", 25, PokemonType.Electric, PokemonType.None,
                hp: 35, atk: 55, def: 40, spAtk: 50, spDef: 50, speed: 90,
                catchRate: 190, expYield: 82,
                learnset: new[] { (1, quickAttack), (1, growl), (6, thunderShock) });

            // ----- Items -----
            var captureBall = CreateItem("Capture Ball", ItemCategory.Ball,   price: 200, ballMod: 1.0f, heal: 0,
                "A ball for catching wild Pokémon. Standard rate.");
            var greatBall   = CreateItem("Great Ball",   ItemCategory.Ball,   price: 600, ballMod: 1.5f, heal: 0,
                "A better ball with a higher catch rate.");
            var potion      = CreateItem("Potion",       ItemCategory.Potion, price: 300, ballMod: 0,    heal: 20,
                "Restores 20 HP to a single Pokémon.");
            var superPotion = CreateItem("Super Potion", ItemCategory.Potion, price: 700, ballMod: 0,    heal: 50,
                "Restores 50 HP to a single Pokémon.");

            // ----- Encounter table (for Route 1) -----
            var route1Table = CreateEncounterTable("Route1Encounters", 0.12f,
                new[] {
                    (meadowhopper, 2, 4, 10),
                });

            // ----- Data registry (for save system) -----
            var registry = ScriptableObject.CreateInstance<PokemonDataRegistry>();
            registry.species.AddRange(new[] { sproutling, kindling, droplet, meadowhopper, sparkmouse });
            registry.moves  .AddRange(new[] { tackle, scratch, ember, waterGun, vineWhip, quickAttack, growl, thunderShock });
            registry.items  .AddRange(new[] { captureBall, greatBall, potion, superPotion });
            WriteAsset(registry, RegistryPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PokeRed] Sample data generated under Assets/Data/.");
            EditorUtility.DisplayDialog("PokeRed", "Örnek veriler oluşturuldu.\n\nAssets/Data altında species, moves, items, encounters ve registry asset'leri hazır.", "Tamam");
        }

        // ---------- Factories ----------

        private static MoveData CreateMove(string name, PokemonType type, MoveCategory cat,
                                           int power, int accuracy, int pp, int priority)
        {
            var m = ScriptableObject.CreateInstance<MoveData>();
            m.moveName = name;
            m.type = type; m.category = cat;
            m.power = power; m.accuracy = accuracy; m.pp = pp; m.priority = priority;
            m.description = $"{name}: {power} power, {accuracy}% accuracy.";
            WriteAsset(m, $"{MovesDir}/{ToFileName(name)}.asset");
            return m;
        }

        private static PokemonData CreateSpecies(string name, int dex, PokemonType t1, PokemonType t2,
            int hp, int atk, int def, int spAtk, int spDef, int speed,
            int catchRate, int expYield, (int lvl, MoveData move)[] learnset)
        {
            var s = ScriptableObject.CreateInstance<PokemonData>();
            s.speciesName = name; s.dexNumber = dex;
            s.type1 = t1; s.type2 = t2;
            s.baseHP = hp; s.baseAtk = atk; s.baseDef = def;
            s.baseSpAtk = spAtk; s.baseSpDef = spDef; s.baseSpeed = speed;
            s.catchRate = catchRate; s.baseExpYield = expYield;
            foreach (var (lvl, move) in learnset)
                s.learnset.Add(new LevelMove { level = lvl, move = move });
            WriteAsset(s, $"{SpeciesDir}/{ToFileName(name)}.asset");
            return s;
        }

        private static ItemData CreateItem(string name, ItemCategory cat, int price, float ballMod, int heal, string description)
        {
            var i = ScriptableObject.CreateInstance<ItemData>();
            i.itemName = name; i.category = cat; i.price = price;
            i.ballModifier = ballMod; i.healAmount = heal;
            i.description = description;
            WriteAsset(i, $"{ItemsDir}/{ToFileName(name)}.asset");
            return i;
        }

        private static EncounterTable CreateEncounterTable(string name, float stepChance,
            (PokemonData species, int minLvl, int maxLvl, int weight)[] slots)
        {
            var e = ScriptableObject.CreateInstance<EncounterTable>();
            e.stepEncounterChance = stepChance;
            foreach (var s in slots)
                e.slots.Add(new EncounterSlot { species = s.species, minLevel = s.minLvl, maxLevel = s.maxLvl, weight = s.weight });
            WriteAsset(e, $"{EncountersDir}/{ToFileName(name)}.asset");
            return e;
        }

        // ---------- Infra ----------

        private static void EnsureFolders()
        {
            EnsureFolder(DataDir);
            EnsureFolder(MovesDir);
            EnsureFolder(SpeciesDir);
            EnsureFolder(ItemsDir);
            EnsureFolder(EncountersDir);
            EnsureFolder(TrainersDir);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var name   = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void WriteAsset(Object obj, string path)
        {
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(obj, path);
        }

        private static string ToFileName(string name) => name.Replace(' ', '_');
    }
}
#endif
