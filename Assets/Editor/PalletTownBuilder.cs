#if UNITY_EDITOR
using PokeRed.Core;
using PokeRed.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static PokeRed.EditorTools.SceneBuilderUtils;

namespace PokeRed.EditorTools
{
    /// <summary>
    /// Builds the first real map — a small town laid out as a 20×14 grid bounded
    /// by trees, with two buildings, a handful of NPCs, a sign, and a warp going
    /// north to Route 1. Placeholder-colour sprites and original dialogue lines.
    /// </summary>
    public static class PalletTownBuilder
    {
        private const string ScenePath = "Assets/Scenes/PalletTown.unity";

        [MenuItem("PokeRed/Setup/Build Pallet Town")]
        public static void Build()
        {
            int solidLayer        = RequireLayer("Solid",        SolidLayerIndex);
            int interactableLayer = RequireLayer("Interactable", InteractableLayer);

            var treeSprite    = GetOrCreateSolidSprite("tree",    new Color(0.12f, 0.35f, 0.18f));
            var pathSprite    = GetOrCreateSolidSprite("path",    new Color(0.70f, 0.62f, 0.45f));
            var roof1Sprite   = GetOrCreateSolidSprite("roof1",   new Color(0.75f, 0.25f, 0.25f));
            var roof2Sprite   = GetOrCreateSolidSprite("roof2",   new Color(0.30f, 0.45f, 0.80f));
            var playerSprite  = GetOrCreateSolidSprite("player",  new Color(0.90f, 0.30f, 0.30f));
            var npcMomSprite  = GetOrCreateSolidSprite("npc_mom", new Color(0.85f, 0.55f, 0.70f));
            var npcProfSprite = GetOrCreateSolidSprite("npc_prof",new Color(0.80f, 0.75f, 0.55f));
            var npcKidSprite  = GetOrCreateSolidSprite("npc_kid", new Color(0.30f, 0.55f, 0.95f));
            var signSprite    = GetOrCreateSolidSprite("sign",    new Color(0.70f, 0.50f, 0.20f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var infra = CreateSceneInfra(new Color(0.28f, 0.55f, 0.28f)); // grassy background

            // ---------- Map border (trees) ----------
            const int minX = -10, maxX = 10, minY = -7, maxY = 8;
            for (int x = minX; x <= maxX; x++)
            {
                // Top edge, leave a 2-tile gap for the Route 1 exit.
                if (x < -1 || x > 1) CreateStaticBlock($"TreeTop_{x}", treeSprite, new Vector2(x, maxY), solidLayer);
                CreateStaticBlock($"TreeBot_{x}", treeSprite, new Vector2(x, minY), solidLayer);
            }
            for (int y = minY + 1; y < maxY; y++)
            {
                CreateStaticBlock($"TreeL_{y}", treeSprite, new Vector2(minX, y), solidLayer);
                CreateStaticBlock($"TreeR_{y}", treeSprite, new Vector2(maxX, y), solidLayer);
            }

            // ---------- Buildings ----------
            //  [roof roof roof]
            //  [wall wall wall]  <-- blocks movement
            //  [   door       ]  <-- simple visual only, no enterable interior in MVP
            DrawBuilding("HouseRed",  new Vector2(-5, 1), roof1Sprite, solidLayer, treeSprite);
            DrawBuilding("HouseBlue", new Vector2( 4, 1), roof2Sprite, solidLayer, treeSprite);

            // ---------- Path (decorative, no collision) ----------
            for (int y = -6; y <= 7; y++)
                CreateDecor($"Path_{y}", pathSprite, new Vector2(0, y));
            for (int x = -5; x <= 5; x++)
                CreateDecor($"PathH_{x}", pathSprite, new Vector2(x, -2));

            // ---------- Player ----------
            Vector2 playerStart = new Vector2(0, -4);
            CreatePlayer(playerSprite, playerStart, 1 << solidLayer, 1 << interactableLayer);
            var playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            CreateFollowerCamera(infra.mainCamera, playerTransform);

            // ---------- NPCs ----------
            CreateNPC("NPC_Mom",  npcMomSprite, new Vector2(-5, -2), Direction.Down,
                new[] {
                    "Dikkatli ol sevgilim!",
                    "Dünya beklenmedik şeylerle dolu.",
                    "Her zaman yanımda olduğunu biliyorum."
                }, wander: false, 1 << solidLayer, interactableLayer);

            CreateNPC("NPC_Professor", npcProfSprite, new Vector2(4, -2), Direction.Left,
                new[] {
                    "Merhaba genç kaşif!",
                    "Kuzeyde uzanan Route 1, başlangıç için harika bir yer.",
                    "Yüksek çimenlerde vahşi Pokemon'lara dikkat et."
                }, wander: false, 1 << solidLayer, interactableLayer);

            CreateNPC("NPC_Kid", npcKidSprite, new Vector2(-2, 3), Direction.Down,
                new[] {
                    "Bir gün ben de buradan ayrılıp usta olacağım!",
                    "Ama önce çimenlere bakmalıyım…"
                }, wander: true, 1 << solidLayer, interactableLayer);

            // ---------- Sign ----------
            CreateSign(signSprite, new Vector2(-1, -4), new[] {
                "PALLET TOWN",
                "Yeni yolculukların başladığı sakin bir kasaba."
            }, interactableLayer);

            // ---------- Warp to Route 1 (top gap) ----------
            CreateWarp("WarpToRoute1", new Vector2(0, 7), new Vector2(2, 1), "Route1", "FromPallet");

            // Spawn point for when the player returns from Route 1.
            CreateSpawnPoint("FromRoute1", new Vector2(0, 6), playerTransform);

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

            Debug.Log($"[PokeRed] Pallet Town built at {ScenePath}.");
            EditorUtility.DisplayDialog("PokeRed", "Pallet Town hazır!\n\nPlay → oklarla kuzeye doğru ilerle → Route 1'e warp.", "Tamam");
        }

        /// <summary>3-tile wide 2-tile tall building: roof row on top, solid wall row below.</summary>
        private static void DrawBuilding(string name, Vector2 center, Sprite roofSprite, int solidLayer, Sprite wallSprite)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                CreateDecor($"{name}_roof_{dx}", roofSprite, new Vector2(center.x + dx, center.y + 1));
                CreateStaticBlock($"{name}_wall_{dx}", wallSprite, new Vector2(center.x + dx, center.y), solidLayer);
            }
        }

        /// <summary>Non-blocking visual tile (rendered behind actors).</summary>
        private static void CreateDecor(string name, Sprite sprite, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(pos.x, pos.y, 0.5f); // behind actors at z=0
            var rend = go.AddComponent<SpriteRenderer>();
            AssignSprite(rend, sprite, name);
            rend.sortingOrder = -1;
        }
    }
}
#endif
