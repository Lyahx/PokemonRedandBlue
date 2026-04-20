#if UNITY_EDITOR
using PokeRed.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static PokeRed.EditorTools.SceneBuilderUtils;

namespace PokeRed.EditorTools
{
    /// <summary>Lightweight main menu: title + New Game / Continue / Quit.</summary>
    public static class MainMenuBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";

        [MenuItem("PokeRed/Setup/Build Main Menu")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Just a camera for clear background; no gameplay systems needed here.
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            camGO.transform.position = new Vector3(0, 0, -10);
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.10f, 0.15f);
            camGO.AddComponent<AudioListener>();

            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            var canvasGO = new GameObject("MainMenuCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Title
            var title = CreateText(canvasGO.transform, "Title",
                new Vector2(0.15f, 0.70f), new Vector2(0.85f, 0.90f),
                Vector2.zero, Vector2.zero,
                "POKERED", 96, TextAlignmentOptions.Center);
            title.color = new Color(0.95f, 0.3f, 0.3f);
            title.fontStyle = FontStyles.Bold;

            var subtitle = CreateText(canvasGO.transform, "Subtitle",
                new Vector2(0.15f, 0.63f), new Vector2(0.85f, 0.70f),
                Vector2.zero, Vector2.zero,
                "a placeholder journey begins", 24, TextAlignmentOptions.Center);
            subtitle.color = new Color(0.75f, 0.75f, 0.80f);
            subtitle.fontStyle = FontStyles.Italic;

            // Buttons
            var newGameBtn  = CreateButton(canvasGO.transform, "NewGame",
                new Vector2(0.35f, 0.42f), new Vector2(0.65f, 0.50f), "NEW GAME");
            var continueBtn = CreateButton(canvasGO.transform, "Continue",
                new Vector2(0.35f, 0.32f), new Vector2(0.65f, 0.40f), "CONTINUE");
            var quitBtn     = CreateButton(canvasGO.transform, "Quit",
                new Vector2(0.35f, 0.22f), new Vector2(0.65f, 0.30f), "QUIT");

            // Controller
            var controllerGO = new GameObject("MainMenuController");
            var controller   = controllerGO.AddComponent<MainMenuController>();
            SetField(controller, "newGameButton",  newGameBtn);
            SetField(controller, "continueButton", continueBtn);
            SetField(controller, "quitButton",     quitBtn);
            SetStringField(controller, "newGameScene", "PalletTown");

            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"[PokeRed] Main Menu built at {ScenePath}.");
            EditorUtility.DisplayDialog("PokeRed", "Main Menu hazır.\n\nNEW GAME → PalletTown'a gider.", "Tamam");
        }
    }
}
#endif
