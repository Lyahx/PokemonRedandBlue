#if UNITY_EDITOR
using System.IO;
using PokeRed.Battle;
using PokeRed.Core;
using PokeRed.DebugTools;
using PokeRed.Dialogue;
using PokeRed.NPC;
using PokeRed.Player;
using PokeRed.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PokeRed.EditorTools
{
    /// <summary>
    /// Builds a minimal but fully wired test scene so the project can be played
    /// without any manual component wiring. Run via <c>PokeRed/Setup/Build Test Scene</c>.
    /// </summary>
    public static class TestSceneBuilder
    {
        private const string SceneDir    = "Assets/Scenes";
        private const string ScenePath   = "Assets/Scenes/Test.unity";
        private const string SpritesDir  = "Assets/Sprites/Generated";

        [MenuItem("PokeRed/Setup/Re-apply Sprite References")]
        public static void ReapplySprites()
        {
            EnsureFolder("Assets/Sprites");
            EnsureFolder(SpritesDir);

            var playerSprite = GetOrCreateSquareSprite("player", new Color(0.90f, 0.30f, 0.30f));
            var npcSprite    = GetOrCreateSquareSprite("npc",    new Color(0.30f, 0.55f, 0.95f));
            var wallSprite   = GetOrCreateSquareSprite("wall",   new Color(0.45f, 0.45f, 0.50f));
            var signSprite   = GetOrCreateSquareSprite("sign",   new Color(0.70f, 0.50f, 0.20f));

            int fixedCount = 0;
            foreach (var rend in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                string n = rend.gameObject.name.ToLowerInvariant();
                Sprite assign = null;
                if (n == "player")           assign = playerSprite;
                else if (n.StartsWith("npc"))  assign = npcSprite;
                else if (n.StartsWith("wall")) assign = wallSprite;
                else if (n == "sign")          assign = signSprite;

                if (assign != null && rend.sprite != assign)
                {
                    AssignSprite(rend, assign, rend.gameObject.name);
                    fixedCount++;
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[PokeRed] Re-applied sprites on {fixedCount} renderers.");
        }

        [MenuItem("PokeRed/Setup/Build Test Scene")]
        public static void Build()
        {
            EnsureFolder(SceneDir);
            EnsureFolder("Assets/Sprites");
            EnsureFolder(SpritesDir);

            int solidLayer        = RequireLayer("Solid",        6);
            int interactableLayer = RequireLayer("Interactable", 7);

            var playerSprite = GetOrCreateSquareSprite("player", new Color(0.90f, 0.30f, 0.30f));
            var npcSprite    = GetOrCreateSquareSprite("npc",    new Color(0.30f, 0.55f, 0.95f));
            var wallSprite   = GetOrCreateSquareSprite("wall",   new Color(0.45f, 0.45f, 0.50f));
            var signSprite   = GetOrCreateSquareSprite("sign",   new Color(0.70f, 0.50f, 0.20f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateEventSystem();

            var gmGO = new GameObject("GameManager");
            gmGO.AddComponent<GameManager>();

            // Dialogue UI first, so DialogueManager can reference its box.
            var dialogueBox = CreateDialogueCanvas(out var dialogueBoxComponent);

            var dialogueMgrGO = new GameObject("DialogueManager");
            var dialogueMgr   = dialogueMgrGO.AddComponent<DialogueManager>();
            SetObjField(dialogueMgr, "box", dialogueBoxComponent);

            var battleCanvas  = CreateBattleCanvas(out var battleUi);
            var battleSysGO   = new GameObject("BattleSystem");
            var battleSys     = battleSysGO.AddComponent<BattleSystem>();
            SetObjField(battleSys, "battleUiBehaviour", battleUi);

            // World
            CreateWall(new Vector2( 4,  0), wallSprite, solidLayer);
            CreateWall(new Vector2(-4,  0), wallSprite, solidLayer);
            CreateWall(new Vector2( 0,  3), wallSprite, solidLayer);
            CreateWall(new Vector2( 0, -3), wallSprite, solidLayer);
            CreateWall(new Vector2( 2,  2), wallSprite, solidLayer);
            CreateWall(new Vector2(-2, -2), wallSprite, solidLayer);

            // Player
            var player = CreateSpriteActor("Player", playerSprite, new Vector2(0, 0));
            player.tag = "Player";
            var gridMover = player.AddComponent<GridMover>();
            SetLayerMaskField(gridMover, "solidLayer", 1 << solidLayer);
            var pc = player.AddComponent<PlayerController>();
            SetLayerMaskField(pc, "interactLayer", 1 << interactableLayer);

            // NPC
            var npc = CreateSpriteActor("NPC_Villager", npcSprite, new Vector2(3, 1), interactableLayer);
            var npcMover = npc.AddComponent<GridMover>();
            SetLayerMaskField(npcMover, "solidLayer", 1 << solidLayer);
            var npcCtrl = npc.AddComponent<NPCController>();
            SetStringList(npcCtrl, "lines", new[]
            {
                "Merhaba! Bu PokeRed test sahnesi.",
                "B tuşuna basarak test savaşını tetikleyebilirsin.",
                "Oklar/WASD ile hareket, Z/Enter ile etkileşim."
            });

            // Sign
            var sign = new GameObject("Sign");
            sign.transform.position = new Vector3(-3, 2, 0);
            sign.layer = interactableLayer;
            var signRend = sign.AddComponent<SpriteRenderer>();
            AssignSprite(signRend, signSprite, sign.name);
            var signCol = sign.AddComponent<BoxCollider2D>();
            signCol.isTrigger = false;
            var signScript = sign.AddComponent<SignInteractable>();
            SetStringList(signScript, "lines", new[] { "Burası PokeRed test tabelasıdır." });

            // Camera follow: simple child of Player so it tracks on movement.
            var cam = Camera.main;
            if (cam != null)
            {
                var follow = cam.gameObject.AddComponent<FollowTarget>();
                SetObjField(follow, "target", player.transform);
            }

            // Debug helpers
            var debugGO = new GameObject("Debug");
            debugGO.AddComponent<DebugBootstrap>();
            debugGO.AddComponent<BattleTrigger>();

            // Verify every SpriteRenderer actually got a sprite before persisting.
            int missingSprites = 0;
            foreach (var rend in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (rend.sprite == null)
                {
                    Debug.LogError($"[PokeRed] SpriteRenderer on '{rend.gameObject.name}' has no sprite after build.");
                    missingSprites++;
                }
            }

            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            // Make sure the scene is in Build Settings so warp/load works later.
            AddSceneToBuildSettings(ScenePath);

            string summary = missingSprites == 0
                ? $"Test sahnesi hazır!\n\nWASD/Oklar → hareket\nZ/Enter → etkileşim\nB → vahşi savaş"
                : $"Sahne kuruldu ama {missingSprites} SpriteRenderer boş — 'Re-apply Sprite References' menüsünü çalıştır.";
            Debug.Log($"[PokeRed] Test scene built at {ScenePath}. Missing sprites: {missingSprites}");
            EditorUtility.DisplayDialog("PokeRed", summary, "Tamam");
        }

        // ---------- Builders ----------

        private static void CreateCamera()
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            camGO.transform.position = new Vector3(0, 0, -10);
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.12f, 0.14f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane  = 50f;
            camGO.AddComponent<AudioListener>();
        }

        private static void CreateEventSystem()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        private static GameObject CreateSpriteActor(string name, Sprite sprite, Vector2 pos, int layer = 0)
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

        private static void CreateWall(Vector2 pos, Sprite sprite, int layer)
        {
            var go = new GameObject($"Wall_{pos.x}_{pos.y}");
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            go.layer = layer;
            var rend = go.AddComponent<SpriteRenderer>();
            AssignSprite(rend, sprite, go.name);
            go.AddComponent<BoxCollider2D>();
        }

        private static void AssignSprite(SpriteRenderer rend, Sprite sprite, string owner)
        {
            if (sprite == null)
            {
                Debug.LogWarning($"[PokeRed] '{owner}' created without a sprite — load probably failed.");
                return;
            }
            rend.sprite = sprite;
            var so = new SerializedObject(rend);
            so.FindProperty("m_Sprite").objectReferenceValue = sprite;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(rend);
        }

        // ---------- Dialogue Canvas ----------

        private static GameObject CreateDialogueCanvas(out DialogueBox box)
        {
            var canvasGO = new GameObject("DialogueCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRt = panelGO.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.05f, 0.05f);
            panelRt.anchorMax = new Vector2(0.95f, 0.28f);
            panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;
            var panelImg = panelGO.AddComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0.78f);

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(panelGO.transform, false);
            var labelRt = labelGO.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(20, 15);
            labelRt.offsetMax = new Vector2(-20, -15);
            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = "";
            label.fontSize = 28;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.Normal;

            box = canvasGO.AddComponent<DialogueBox>();
            SetObjField(box, "root",  panelGO);
            SetObjField(box, "label", label);
            panelGO.SetActive(false);

            return canvasGO;
        }

        // ---------- Battle Canvas ----------

        private static GameObject CreateBattleCanvas(out BattleUI battleUi)
        {
            var canvasGO = new GameObject("BattleCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            var root = CreatePanel("BattleRoot", canvasGO.transform, Vector2.zero, Vector2.one, new Color(0.15f, 0.15f, 0.20f, 1f));

            // Enemy panel (top-left)
            var enemyPanel = CreatePanel("EnemyPanel", root.transform,
                new Vector2(0.03f, 0.62f), new Vector2(0.48f, 0.92f), new Color(0,0,0,0.5f));
            var enemyName  = CreateText(enemyPanel.transform, "EnemyName",  new Vector2(0.05f,0.6f), new Vector2(0.7f,0.95f), "Enemy", 24, TextAlignmentOptions.MidlineLeft);
            var enemyLevel = CreateText(enemyPanel.transform, "EnemyLevel", new Vector2(0.72f,0.6f), new Vector2(0.98f,0.95f), "L?", 22, TextAlignmentOptions.MidlineRight);
            var enemyBar   = CreateSlider(enemyPanel.transform, "EnemyHP", new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.5f));

            // Player panel (bottom-right-ish)
            var playerPanel = CreatePanel("PlayerPanel", root.transform,
                new Vector2(0.50f, 0.38f), new Vector2(0.97f, 0.62f), new Color(0,0,0,0.5f));
            var playerName   = CreateText(playerPanel.transform, "PlayerName",   new Vector2(0.05f,0.65f), new Vector2(0.6f,0.98f), "Player", 24, TextAlignmentOptions.MidlineLeft);
            var playerLevel  = CreateText(playerPanel.transform, "PlayerLevel",  new Vector2(0.62f,0.65f), new Vector2(0.98f,0.98f), "L?", 22, TextAlignmentOptions.MidlineRight);
            var playerHPText = CreateText(playerPanel.transform, "PlayerHPText", new Vector2(0.05f,0.05f), new Vector2(0.98f,0.35f), "HP", 20, TextAlignmentOptions.MidlineRight);
            var playerBar    = CreateSlider(playerPanel.transform, "PlayerHP", new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.62f));

            // Message panel (bottom)
            var messagePanel = CreatePanel("MessagePanel", root.transform,
                new Vector2(0.03f, 0.03f), new Vector2(0.65f, 0.33f), new Color(0,0,0,0.78f));
            var messageLabel = CreateText(messagePanel.transform, "MessageLabel",
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), "", 26, TextAlignmentOptions.TopLeft);
            messageLabel.textWrappingMode = TextWrappingModes.Normal;

            // Action menu (bottom-right)
            var actionMenu = CreatePanel("ActionMenu", root.transform,
                new Vector2(0.67f, 0.03f), new Vector2(0.97f, 0.33f), new Color(0,0,0,0.78f));
            var fightButton = CreateButton(actionMenu.transform, "Fight",  new Vector2(0.05f,0.55f), new Vector2(0.48f,0.95f), "FIGHT");
            var bagButton   = CreateButton(actionMenu.transform, "Bag",    new Vector2(0.52f,0.55f), new Vector2(0.95f,0.95f), "BAG");
            var partyButton = CreateButton(actionMenu.transform, "Party",  new Vector2(0.05f,0.05f), new Vector2(0.48f,0.45f), "PKMN");
            var runButton   = CreateButton(actionMenu.transform, "Run",    new Vector2(0.52f,0.05f), new Vector2(0.95f,0.45f), "RUN");

            // Move menu (replaces action menu when Fight clicked)
            var moveMenu = CreatePanel("MoveMenu", root.transform,
                new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.33f), new Color(0,0,0,0.85f));
            var moveButtons  = new Button[4];
            var moveLabels   = new TMP_Text[4];
            var movePPLabels = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                float col = i % 2;
                float row = 1 - (i / 2);
                var xMin = 0.03f + col * 0.49f;
                var xMax = xMin + 0.46f;
                var yMin = 0.05f + row * 0.48f;
                var yMax = yMin + 0.43f;
                moveButtons[i]  = CreateButton(moveMenu.transform, $"MoveBtn{i}", new Vector2(xMin, yMin), new Vector2(xMax, yMax), $"Move {i+1}");
                moveLabels[i]   = moveButtons[i].GetComponentInChildren<TMP_Text>();
                // PP label to the right corner of the button
                movePPLabels[i] = CreateText(moveButtons[i].transform,
                    "PP",
                    new Vector2(0.55f, 0.05f), new Vector2(0.98f, 0.45f),
                    "", 16, TextAlignmentOptions.MidlineRight);
            }

            battleUi = canvasGO.AddComponent<BattleUI>();
            SetObjField(battleUi, "battleRoot",    root);
            SetObjField(battleUi, "messagePanel",  messagePanel);
            SetObjField(battleUi, "actionMenu",    actionMenu);
            SetObjField(battleUi, "moveMenu",      moveMenu);
            SetObjField(battleUi, "messageLabel",  messageLabel);
            SetObjField(battleUi, "playerName",    playerName);
            SetObjField(battleUi, "playerLevel",   playerLevel);
            SetObjField(battleUi, "playerHPText",  playerHPText);
            SetObjField(battleUi, "playerHPBar",   playerBar);
            SetObjField(battleUi, "enemyName",     enemyName);
            SetObjField(battleUi, "enemyLevel",    enemyLevel);
            SetObjField(battleUi, "enemyHPBar",    enemyBar);
            SetObjField(battleUi, "fightButton",   fightButton);
            SetObjField(battleUi, "bagButton",     bagButton);
            SetObjField(battleUi, "partyButton",   partyButton);
            SetObjField(battleUi, "runButton",     runButton);
            SetArrayField(battleUi, "moveButtons",  moveButtons);
            SetArrayField(battleUi, "moveLabels",   moveLabels);
            SetArrayField(battleUi, "movePPLabels", movePPLabels);

            root.SetActive(false);
            return canvasGO;
        }

        // ---------- UI helpers ----------

        private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static TMP_Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string text, int size, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            return t;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.05f, 0.05f, 1f);
            var slider = go.AddComponent<Slider>();
            slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            // Fill area
            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(go.transform, false);
            var fRt = fillArea.AddComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero; fRt.anchorMax = Vector2.one;
            fRt.offsetMin = new Vector2(2,2); fRt.offsetMax = new Vector2(-2,-2);

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

        private static Button CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string label)
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
            t.text = label; t.fontSize = 22; t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;

            return btn;
        }

        // ---------- Sprites ----------

        private static Sprite GetOrCreateSquareSprite(string name, Color color)
        {
            string path = $"{SpritesDir}/{name}.png";

            // Regenerate the PNG every build so rebuilds stay in sync with code changes.
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            // Force a synchronous import so the importer is ready before we configure it.
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp != null)
            {
                imp.textureType           = TextureImporterType.Sprite;
                imp.spritePixelsPerUnit   = 16;
                imp.filterMode            = FilterMode.Point;
                imp.textureCompression    = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled         = false;
                imp.isReadable            = false;
                imp.alphaIsTransparency   = true;
                imp.SaveAndReimport();
            }

            // Reimport once more with the new Sprite settings, synchronously.
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogError($"[PokeRed] Sprite load failed at {path}. SpriteRenderer will be empty.");
            return sprite;
        }

        // ---------- Infra ----------

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var name   = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static int RequireLayer(string layerName, int fallbackIndex)
        {
            int idx = LayerMask.NameToLayer(layerName);
            if (idx >= 0) return idx;
            Debug.LogWarning($"[PokeRed] Layer '{layerName}' bulunamadı. User Layer {fallbackIndex}'te oluşturman gerekli. Şimdilik 'Default' kullanıyorum.");
            return 0;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes) if (s.path == scenePath) return;
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
            list.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static void SetObjField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"Field '{field}' not found on {target.GetType().Name}"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        private static void SetLayerMaskField(Object target, string field, int mask)
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(field);
            if (p == null) return;
            p.intValue = mask;
            so.ApplyModifiedProperties();
        }

        private static void SetArrayField<T>(Object target, string field, T[] values) where T : Object
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"Array field '{field}' not found on {target.GetType().Name}"); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedProperties();
        }

        private static void SetStringList(Object target, string field, string[] values)
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"List<string> field '{field}' not found on {target.GetType().Name}"); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                p.GetArrayElementAtIndex(i).stringValue = values[i];
            so.ApplyModifiedProperties();
        }
    }
}
#endif
