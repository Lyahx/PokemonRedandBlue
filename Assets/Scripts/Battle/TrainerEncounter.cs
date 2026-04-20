using System.Collections;
using PokeRed.Core;
using PokeRed.Dialogue;
using PokeRed.NPC;
using UnityEngine;

namespace PokeRed.Battle
{
    /// <summary>
    /// Overworld trainer: interact to open the pre-battle lines, then battle begins.
    /// Attach next to an NPCController/Collider2D on the Interactable layer.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TrainerEncounter : MonoBehaviour, IInteractable
    {
        [SerializeField] private TrainerData trainer;
        [SerializeField] private bool defeated;

        public void Interact(Direction from)
        {
            if (trainer == null || defeated) return;
            StartCoroutine(RunEncounter());
        }

        private IEnumerator RunEncounter()
        {
            if (DialogueManager.Instance != null && trainer.preBattleLines.Count > 0)
            {
                DialogueManager.Instance.Show(trainer.preBattleLines);
                while (GameManager.Instance != null && GameManager.Instance.State == GameState.Dialogue)
                    yield return null;
            }

            if (GameManager.Instance == null || !GameManager.Instance.PlayerParty.HasUsable) yield break;
            if (BattleSystem.Instance == null) yield break;

            BattleSystem.Instance.StartTrainerBattle(GameManager.Instance.PlayerParty, trainer);

            while (GameManager.Instance.State == GameState.Battle) yield return null;

            bool playerWon = GameManager.Instance.PlayerParty.HasUsable;
            if (!playerWon) yield break; // Player blacked out; allow a rematch on reload.

            defeated = true;

            if (DialogueManager.Instance != null && trainer.defeatedByLines.Count > 0)
                DialogueManager.Instance.Show(trainer.defeatedByLines);
        }
    }
}
