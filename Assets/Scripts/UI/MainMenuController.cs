using PokeRed.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PokeRed.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string newGameScene = "PalletTown";
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button quitButton;

        private void Start()
        {
            if (continueButton != null)
                continueButton.interactable = SaveSystem.Instance != null && SaveSystem.Instance.Exists();

            if (newGameButton  != null) newGameButton.onClick.AddListener(OnNewGame);
            if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
            if (quitButton     != null) quitButton.onClick.AddListener(OnQuit);
        }

        private void OnNewGame() => SceneManager.LoadScene(newGameScene);

        private void OnContinue()
        {
            if (SaveSystem.Instance != null) SaveSystem.Instance.Load();
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
