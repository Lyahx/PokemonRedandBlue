#if UNITY_EDITOR
using System.IO;
using PokeRed.Battle;
using PokeRed.Core;
using PokeRed.Dialogue;
using PokeRed.NPC;
using PokeRed.Player;
using PokeRed.World;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PokeRed.EditorTools
{
    /// <summary>
    /// Shared helpers used by per-scene builders so every map gets the same
    /// camera/UI/managers without duplicating wiring. Kept internal to the
    /// editor assembly.
    /// </summary>
    public static class SceneBuilderUtils
    {
        private const string GeneratedSpritesDir = "Assets/Sprites/Generated";

        public const int DefaultLayer      = 0;
        public const int SolidLayerIndex   = 6;
        public const int InteractableLayer = 7;

        public struct SceneInfra
        {
            public Camera        mainCamera;
            public GameManager   gameManager;
            public DialogueBox   dialogueBox;
            public DialogueManager dialogueManager;
            public BattleSystem  battleSystem;
            public BattleUI      battleUi;
        }

        // ---------- Folders / Sprites ----------

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var name   = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        public static Sprite GetOrCreateSolidSprite(string name, Color color)
        {
            EnsureFolder("Assets/Sprites");
            EnsureFolder(GeneratedSpritesDir);
            string path = $"{GeneratedSpritesDir}/{name}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var existingTex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null && existingTex != null) return existing;

            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false)
            {
                name = name + "_tex",
                filterMode = FilterMode.Point,
                wrapMode   = TextureWrapMode.Clamp,
                alphaIsTransparency = true
            };
            var pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
            sprite.name = name;

            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(tex, path);
            AssetDatabase.AddObjectToAsset(sprite, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var loaded = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (loaded == null)
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (o is Sprite s) { loaded = s; break; }
            return loaded;
        }

        public static void AssignSprite(SpriteRenderer rend, Sprite sprite, string owner)
        {
            if (sprite == null)
            {
                Debug.LogWarning($"[PokeRed] '{owner}' created without a sprite — load failed.");
                return;
            }
            rend.sprite = sprite;
            var so = new SerializedObject(rend);
            so.FindProperty("m_Sprite").objectReferenceValue = sprite;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(rend);
        }

        // ---------- Scene-level infra (camera, event system, managers, canvases) ----------

        public static SceneInfra CreateSceneInfra(Color backgroundColor, float cameraSize = 5f)
        {
            var infra = new SceneInfra();

            // Camera
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            camGO.transform.position = new Vector3(0, 0, -10);
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = cameraSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane  = 50f;
            camGO.AddComponent<AudioListener>();
            infra.mainCamera = cam;

            // EventSystem
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();

            // GameManager
            var gmGO = new GameObject("GameManager");
            infra.gameManager = gmGO.AddComponent<GameManager>();

            // Dialogue UI + manager
            infra.dialogueBox = CreateDialogueCanvas();
            var dmGO = new GameObject("DialogueManager");
            infra.dialogueManager = dmGO.AddComponent<DialogueManager>();
            SetField(infra.dialogueManager, "box", infra.dialogueBox);

            // Battle UI + system
            infra.battleUi = CreateBattleCanvas();
            var bsGO = new GameObject("BattleSystem");
            infra.battleSystem = bsGO.AddComponent<BattleSystem>();
            SetField(infra.battleSystem, "battleUiBehaviour", infra.battleUi);

            return infra;
        }

        // ---------- Entities ----------

        public static GameObject CreateSpriteActor(string name, Sprite sprite, Vector2 pos, int layer = 0)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            go.layer = layer;
            var rend = go.AddComponent<SpriteRenderer>();
            AssignSprite(rend, sprite, name);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * 0.9f;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;
            return go;
        }

        public static GameObject CreateStaticBlock(string name, Sprite sprite, Vector2 pos, int layer, Vector2? size = null)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            go.layer = layer;
            var rend = go.AddComponent<SpriteRenderer>();
            AssignSprite(rend, sprite, name);
            if (size.HasValue) go.transform.localScale = new Vector3(size.Value.x, size.Value.y, 1f);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one; // collider in local space — scaled with transform
            return go;
        }

        public static void CreatePlayer(Sprite sprite, Vector2 pos, int solidMask, int interactMask)
        {
            var player = CreateSpriteActor("Player", sprite, pos);
            player.tag = "Player";
            var mover = player.AddComponent<GridMover>();
            SetLayerMaskField(mover, "solidLayer", solidMask);
            var pc = player.AddComponent<PlayerController>();
            SetLayerMaskField(pc, "interactLayer", interactMask);
        }

        public static GameObject CreateNPC(string name, Sprite sprite, Vector2 pos, Direction facing,
                                           string[] lines, bool wander, int solidMask, int interactableLayer)
        {
            var npc = CreateSpriteActor(name, sprite, pos, interactableLayer);
            var mover = npc.AddComponent<GridMover>();
            SetLayerMaskField(mover, "solidLayer", solidMask);
            var ctrl = npc.AddComponent<NPCController>();
            SetEnumField(ctrl, "startFacing", (int)facing);
            SetStringList(ctrl, "lines", lines);
            SetBoolField(ctrl, "wander", wander);
            return npc;
        }

        public static GameObject CreateTrainerNPC(string name, Sprite sprite, Vector2 pos,
                                                   TrainerData trainerAsset, int interactableLayer)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            go.layer = interactableLayer;
            var rend = go.AddComponent<SpriteRenderer>();
            AssignSprite(rend, sprite, name);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * 0.9f;
            var enc = go.AddComponent<TrainerEncounter>();
            SetField(enc, "trainer", trainerAsset);
            return go;
        }

        public static GameObject CreateSign(Sprite sprite, Vector2 pos, string[] lines, int interactableLayer)
        {
            var go = new GameObject($"Sign_{pos.x}_{pos.y}");
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            go.layer = interactableLayer;
            var rend = go.AddComponent<SpriteRenderer>();
            AssignSprite(rend, sprite, go.name);
            go.AddComponent<BoxCollider2D>();
            var sign = go.AddComponent<SignInteractable>();
            SetStringList(sign, "lines", lines);
            return go;
        }

        public static GameObject CreateWarp(string name, Vector2 pos, Vector2 size, string targetScene, string spawnId)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
            var warp = go.AddComponent<Warp>();
            SetStringField(warp, "targetScene",   targetScene);
            SetStringField(warp, "targetSpawnId", spawnId);
            return go;
        }

        public static GameObject CreateSpawnPoint(string id, Vector2 pos, Transform player)
        {
            var go = new GameObject($"Spawn_{id}");
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            var sp = go.AddComponent<SpawnPoint>();
            SetStringField(sp, "id", id);
            SetField(sp, "player", player);
            return go;
        }

        public static GameObject CreateEncounterZone(string name, Vector2 pos, Vector2 size, EncounterTable table, int encounterLayer)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            go.layer = encounterLayer;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
            var zone = go.AddComponent<EncounterZone>();
            SetField(zone, "table", table);
            return go;
        }

        public static GameObject CreateFollowerCamera(Camera cam, Transform target)
        {
            var follow = cam.gameObject.AddComponent<FollowTarget>();
            SetField(follow, "target", target);
            return cam.gameObject;
        }

        // ---------- UI canvases ----------

        private static DialogueBox CreateDialogueCanvas()
        {
            var canvasGO = new GameObject("DialogueCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            var panel = CreatePanel("Panel", canvasGO.transform,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.28f), new Color(0, 0, 0, 0.78f));

            var label = CreateText(panel.transform, "Label",
                new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(20, 15), new Vector2(-20, -15),
                "", 28, TextAlignmentOptions.TopLeft);
            label.textWrappingMode = TextWrappingModes.Normal;

            var box = canvasGO.AddComponent<DialogueBox>();
            SetField(box, "root", panel);
            SetField(box, "label", label);
            panel.SetActive(false);
            return box;
        }

        private static BattleUI CreateBattleCanvas()
        {
            var canvasGO = new GameObject("BattleCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            var root = CreatePanel("BattleRoot", canvasGO.transform, Vector2.zero, Vector2.one, new Color(0.15f, 0.15f, 0.20f, 1f));

            var enemyPanel  = CreatePanel("EnemyPanel",  root.transform, new Vector2(0.03f, 0.62f), new Vector2(0.48f, 0.92f), new Color(0, 0, 0, 0.5f));
            var enemyName   = CreateText(enemyPanel.transform, "EnemyName",  new Vector2(0.05f, 0.6f), new Vector2(0.7f, 0.95f), Vector2.zero, Vector2.zero, "Enemy", 24, TextAlignmentOptions.MidlineLeft);
            var enemyLevel  = CreateText(enemyPanel.transform, "EnemyLevel", new Vector2(0.72f, 0.6f), new Vector2(0.98f, 0.95f), Vector2.zero, Vector2.zero, "L?", 22, TextAlignmentOptions.MidlineRight);
            var enemyBar    = CreateSlider(enemyPanel.transform, "EnemyHP",  new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.5f));

            var playerPanel  = CreatePanel("PlayerPanel", root.transform, new Vector2(0.50f, 0.38f), new Vector2(0.97f, 0.62f), new Color(0, 0, 0, 0.5f));
            var playerName   = CreateText(playerPanel.transform, "PlayerName",   new Vector2(0.05f, 0.65f), new Vector2(0.6f, 0.98f),  Vector2.zero, Vector2.zero, "Player", 24, TextAlignmentOptions.MidlineLeft);
            var playerLevel  = CreateText(playerPanel.transform, "PlayerLevel",  new Vector2(0.62f, 0.65f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero, "L?", 22, TextAlignmentOptions.MidlineRight);
            var playerHPText = CreateText(playerPanel.transform, "PlayerHPText", new Vector2(0.05f, 0.05f), new Vector2(0.98f, 0.35f), Vector2.zero, Vector2.zero, "HP", 20, TextAlignmentOptions.MidlineRight);
            var playerBar    = CreateSlider(playerPanel.transform, "PlayerHP",   new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.62f));

            var messagePanel = CreatePanel("MessagePanel", root.transform, new Vector2(0.03f, 0.03f), new Vector2(0.65f, 0.33f), new Color(0, 0, 0, 0.78f));
            var messageLabel = CreateText(messagePanel.transform, "MessageLabel", new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), Vector2.zero, Vector2.zero, "", 26, TextAlignmentOptions.TopLeft);
            messageLabel.textWrappingMode = TextWrappingModes.Normal;

            var actionMenu = CreatePanel("ActionMenu", root.transform, new Vector2(0.67f, 0.03f), new Vector2(0.97f, 0.33f), new Color(0, 0, 0, 0.78f));
            var fightButton = CreateButton(actionMenu.transform, "Fight", new Vector2(0.05f, 0.55f), new Vector2(0.48f, 0.95f), "FIGHT");
            var bagButton   = CreateButton(actionMenu.transform, "Bag",   new Vector2(0.52f, 0.55f), new Vector2(0.95f, 0.95f), "BAG");
            var partyButton = CreateButton(actionMenu.transform, "Party", new Vector2(0.05f, 0.05f), new Vector2(0.48f, 0.45f), "PKMN");
            var runButton   = CreateButton(actionMenu.transform, "Run",   new Vector2(0.52f, 0.05f), new Vector2(0.95f, 0.45f), "RUN");

            var moveMenu = CreatePanel("MoveMenu", root.transform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.33f), new Color(0, 0, 0, 0.85f));
            var moveButtons  = new Button[4];
            var moveLabels   = new TMP_Text[4];
            var movePPLabels = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                float col = i % 2, row = 1 - (i / 2);
                float xMin = 0.03f + col * 0.49f, xMax = xMin + 0.46f;
                float yMin = 0.05f + row * 0.48f, yMax = yMin + 0.43f;
                moveButtons[i] = CreateButton(moveMenu.transform, $"MoveBtn{i}", new Vector2(xMin, yMin), new Vector2(xMax, yMax), $"Move {i + 1}");
                moveLabels[i]  = moveButtons[i].GetComponentInChildren<TMP_Text>();
                movePPLabels[i] = CreateText(moveButtons[i].transform, "PP", new Vector2(0.55f, 0.05f), new Vector2(0.98f, 0.45f), Vector2.zero, Vector2.zero, "", 16, TextAlignmentOptions.MidlineRight);
            }

            // Bag menu — vertical list of 4 item rows + back button
            var bagMenu = CreatePanel("BagMenu", root.transform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.45f), new Color(0, 0, 0, 0.88f));
            var bagItemButtons = new Button[4];
            var bagItemLabels  = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                float yTop = 0.95f - i * 0.17f;
                float yBot = yTop - 0.15f;
                bagItemButtons[i] = CreateButton(bagMenu.transform, $"BagItem{i}",
                    new Vector2(0.05f, yBot), new Vector2(0.75f, yTop), "-");
                bagItemLabels[i] = bagItemButtons[i].GetComponentInChildren<TMP_Text>();
                bagItemLabels[i].alignment = TextAlignmentOptions.MidlineLeft;
            }
            var bagBackButton = CreateButton(bagMenu.transform, "BagBack",
                new Vector2(0.78f, 0.05f), new Vector2(0.97f, 0.25f), "BACK");

            var battleUi = canvasGO.AddComponent<BattleUI>();
            SetField(battleUi, "battleRoot",    root);
            SetField(battleUi, "messagePanel",  messagePanel);
            SetField(battleUi, "actionMenu",    actionMenu);
            SetField(battleUi, "moveMenu",      moveMenu);
            SetField(battleUi, "bagMenu",       bagMenu);
            SetField(battleUi, "messageLabel",  messageLabel);
            SetField(battleUi, "playerName",    playerName);
            SetField(battleUi, "playerLevel",   playerLevel);
            SetField(battleUi, "playerHPText",  playerHPText);
            SetField(battleUi, "playerHPBar",   playerBar);
            SetField(battleUi, "enemyName",     enemyName);
            SetField(battleUi, "enemyLevel",    enemyLevel);
            SetField(battleUi, "enemyHPBar",    enemyBar);
            SetField(battleUi, "fightButton",   fightButton);
            SetField(battleUi, "bagButton",     bagButton);
            SetField(battleUi, "partyButton",   partyButton);
            SetField(battleUi, "runButton",     runButton);
            SetField(battleUi, "bagBackButton", bagBackButton);
            SetArray(battleUi, "moveButtons",    moveButtons);
            SetArray(battleUi, "moveLabels",     moveLabels);
            SetArray(battleUi, "movePPLabels",   movePPLabels);
            SetArray(battleUi, "bagItemButtons", bagItemButtons);
            SetArray(battleUi, "bagItemLabels",  bagItemLabels);
            root.SetActive(false);
            return battleUi;
        }

        // ---------- UI primitives ----------

        public static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        public static TMP_Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
                                          Vector2 offsetMin, Vector2 offsetMax, string text, int size, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.alignment = align; t.color = Color.white;
            return t;
        }

        public static Slider CreateSlider(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.05f, 0.05f, 1f);
            var slider = go.AddComponent<Slider>();
            slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(go.transform, false);
            var fRt = fillArea.AddComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero; fRt.anchorMax = Vector2.one;
            fRt.offsetMin = new Vector2(2, 2); fRt.offsetMax = new Vector2(-2, -2);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRt = fill.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.25f, 0.85f, 0.40f, 1f);
            slider.fillRect = fillRt;
            slider.targetGraphic = fillImg;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        public static Button CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.18f);
            var btn = go.AddComponent<Button>();

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lRt = labelGO.AddComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
            lRt.offsetMin = lRt.offsetMax = Vector2.zero;
            var t = labelGO.AddComponent<TextMeshProUGUI>();
            t.text = label; t.fontSize = 22; t.alignment = TextAlignmentOptions.Center; t.color = Color.white;
            return btn;
        }

        // ---------- SerializedObject field setters ----------

        public static void SetField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"Field '{field}' not found on {target.GetType().Name}"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        public static void SetLayerMaskField(Object target, string field, int mask)
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(field);
            if (p == null) return;
            p.intValue = mask;
            so.ApplyModifiedProperties();
        }

        public static void SetEnumField(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(field);
            if (p == null) return;
            p.enumValueIndex = value;
            so.ApplyModifiedProperties();
        }

        public static void SetBoolField(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(field);
            if (p == null) return;
            p.boolValue = value;
            so.ApplyModifiedProperties();
        }

        public static void SetStringField(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(field);
            if (p == null) return;
            p.stringValue = value;
            so.ApplyModifiedProperties();
        }

        public static void SetArray<T>(Object target, string field, T[] values) where T : Object
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(field);
            if (p == null) return;
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedProperties();
        }

        public static void SetStringList(Object target, string field, string[] values)
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(field);
            if (p == null) return;
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                p.GetArrayElementAtIndex(i).stringValue = values[i];
            so.ApplyModifiedProperties();
        }

        // ---------- Layer resolution ----------

        public static int RequireLayer(string layerName, int fallbackIndex)
        {
            int idx = LayerMask.NameToLayer(layerName);
            if (idx >= 0) return idx;
            Debug.LogWarning($"[PokeRed] Layer '{layerName}' missing. Add to User Layer {fallbackIndex}.");
            return 0;
        }

        public static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes) if (s.path == scenePath) return;
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
            list.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif
