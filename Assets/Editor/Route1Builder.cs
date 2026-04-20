#if UNITY_EDITOR
using PokeRed.Core;
using PokeRed.DebugTools;
using PokeRed.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static PokeRed.EditorTools.SceneBuilderUtils;

namespace PokeRed.EditorTools
{
    /// <summary>
    /// A narrow north-south corridor connecting Pallet Town to a future Viridian
    /// City. Contains a tall-grass patch (encounter zone) and two warps — back
    /// to Pallet on the south edge, northern edge blocked for the MVP.
    /// </summary>
    public static class Route1Builder
    {
        private const string ScenePath             = "Assets/Scenes/Route1.unity";
        private const string Route1EncountersPath  = "Assets/Data/Encounters/Route1Encounters.asset";

        [MenuItem("PokeRed/Setup/Build Route 1")]
        public static void Build()
        {
            int solidLayer        = RequireLayer("Solid",        SolidLayerIndex);
            int interactableLayer = RequireLayer("Interactable", InteractableLayer);
            int encounterLayer    = RequireLayer("Encounter",    8);

            var treeSprite    = GetOrCreateSolidSprite("tree",    new Color(0.12f, 0.35f, 0.18f));
            var pathSprite    = GetOrCreateSolidSprite("path",    new Color(0.70f, 0.62f, 0.45f));
            var grassSprite   = GetOrCreateSolidSprite("grass",   new Color(0.35f, 0.70f, 0.35f));
            var playerSprite  = GetOrCreateSolidSprite("player",  new Color(0.90f, 0.30f, 0.30f));
            var signSprite    = GetOrCreateSolidSprite("sign",    new Color(0.70f, 0.50f, 0.20f));
            var npcSprite     = GetOrCreateSolidSprite("npc_hiker", new Color(0.55f, 0.45f, 0.30f));

            var encounterTable = AssetDatabase.LoadAssetAtPath<EncounterTable>(Route1EncountersPath);
            if (encounterTable == null)
                Debug.LogWarning($"[PokeRed] {Route1EncountersPath} missing. Run PokeRed/Setup/Generate Sample Data first.");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var infra = CreateSceneInfra(new Color(0.22f, 0.45f, 0.25f));

            // ---------- Boundary (trees) ----------
            const int minX = -4, maxX = 4, minY = -10, maxY = 10;
            for (int x = minX; x <= maxX; x++)
            {
                // Leave 2-tile gap at the south edge for the warp back to Pallet Town.
                if (x < -1 || x > 1) CreateStaticBlock($"TreeBot_{x}", treeSprite, new Vector2(x, minY), solidLayer);
                // North edge fully blocked (Viridian not built yet).
                CreateStaticBlock($"TreeTop_{x}", treeSprite, new Vector2(x, maxY), solidLayer);
            }
            for (int y = minY + 1; y < maxY; y++)
            {
                CreateStaticBlock($"TreeL_{y}", treeSprite, new Vector2(minX, y), solidLayer);
                CreateStaticBlock($"TreeR_{y}", treeSprite, new Vector2(maxX, y), solidLayer);
            }

            // ---------- Path (decorative) ----------
            for (int y = minY + 1; y < maxY; y++)
                CreateDecor($"Path_{y}", pathSprite, new Vector2(0, y));

            // ---------- Grass patches with encounters ----------
            // Two small patches on either side of the path.
            DrawGrassPatch(new Vector2(-3, 3), 2, 3, grassSprite);
            DrawGrassPatch(new Vector2( 2, -3), 2, 3, grassSprite);

            // Encounter trigger zones that overlay the grass patches.
            if (encounterTable != null)
            {
                CreateEncounterZone("Grass_NW", new Vector2(-2.5f, 4f),   new Vector2(2f, 3f), encounterTable, encounterLayer);
                CreateEncounterZone("Grass_SE", new Vector2( 2.5f, -2f),  new Vector2(2f, 3f), encounterTable, encounterLayer);
            }

            // ---------- Player ----------
            Vector2 playerStart = new Vector2(0, -9);
            CreatePlayer(playerSprite, playerStart, 1 << solidLayer, 1 << interactableLayer);
            var playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            CreateFollowerCamera(infra.mainCamera, playerTransform);

            // ---------- Warp back to Pallet Town ----------
            CreateWarp("WarpToPallet", new Vector2(0, -10), new Vector2(2, 1), "PalletTown", "FromRoute1");

            // Spawn point for arriving from Pallet.
            CreateSpawnPoint("FromPallet", new Vector2(0, -9), playerTransform);

            // ---------- NPC + sign ----------
            CreateNPC("NPC_Hiker", npcSprite, new Vector2(-2, 0), Direction.Right,
                new[] {
                    "Çimenlerin içinde uzun süre kalırsan vahşi Pokemon'lar ortaya çıkar.",
                    "Partinden biriyle onları zayıflatıp yakalamayı dene!"
                }, wander: false, 1 << solidLayer, interactableLayer);

            CreateSign(signSprite, new Vector2(1, -8), new[] {
                "ROUTE 1",
                "Pallet Town ↔ Viridian City"
            }, interactableLayer);

            // ---------- Debug helpers ----------
            var dbg = new GameObject("Debug");
            var bootstrap = dbg.AddComponent<DebugBootstrap>();
            dbg.AddComponent<BattleTrigger>();
            var registry = AssetDatabase.LoadAssetAtPath<PokeRed.Save.PokemonDataRegistry>("Assets/Data/PokemonDataRegistry.asset");
            if (registry != null) SetField(bootstrap, "registry", registry);

            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"[PokeRed] Route 1 built at {ScenePath}.");
            EditorUtility.DisplayDialog("PokeRed", "Route 1 hazır!\n\nÇimenlere gir → vahşi karşılaşma.\nGüneye dön → Pallet Town.", "Tamam");
        }

        private static void DrawGrassPatch(Vector2 center, int halfWidth, int halfHeight, Sprite grassSprite)
        {
            for (int dx = -halfWidth + 1; dx < halfWidth; dx++)
            for (int dy = -halfHeight + 1; dy < halfHeight; dy++)
                CreateDecor($"Grass_{center.x + dx}_{center.y + dy}", grassSprite, new Vector2(center.x + dx, center.y + dy));
        }

        private static void CreateDecor(string name, Sprite sprite, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(pos.x, pos.y, 0.5f);
            var rend = go.AddComponent<SpriteRenderer>();
            AssignSprite(rend, sprite, name);
            rend.sortingOrder = -1;
        }
    }
}
#endif
