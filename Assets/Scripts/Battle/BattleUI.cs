using System;
using System.Collections;
using PokeRed.Core;
using PokeRed.Pokemon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PokeRed.Battle
{
    public class BattleUI : MonoBehaviour, IBattleUI
    {
        [Header("Panels")]
        [SerializeField] private GameObject battleRoot;
        [SerializeField] private GameObject messagePanel;
        [SerializeField] private GameObject actionMenu;
        [SerializeField] private GameObject moveMenu;

        [Header("Message")]
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private float charsPerSecond = 40f;

        [Header("Player side")]
        [SerializeField] private TMP_Text playerName;
        [SerializeField] private TMP_Text playerLevel;
        [SerializeField] private TMP_Text playerHPText;
        [SerializeField] private Slider   playerHPBar;
        [SerializeField] private Image    playerSprite;

        [Header("Enemy side")]
        [SerializeField] private TMP_Text enemyName;
        [SerializeField] private TMP_Text enemyLevel;
        [SerializeField] private Slider   enemyHPBar;
        [SerializeField] private Image    enemySprite;

        [Header("Move buttons")]
        [SerializeField] private Button[]   moveButtons   = new Button[4];
        [SerializeField] private TMP_Text[] moveLabels    = new TMP_Text[4];
        [SerializeField] private TMP_Text[] movePPLabels  = new TMP_Text[4];

        [Header("Action buttons")]
        [SerializeField] private Button fightButton;
        [SerializeField] private Button bagButton;
        [SerializeField] private Button partyButton;
        [SerializeField] private Button runButton;

        private int? actionChoice;      // 0 fight, 1 bag, 2 party, 3 run
        private int? moveChoice;        // 0..3
        private bool continueRequested;

        private void Awake()
        {
            HideAll();
            if (fightButton) fightButton.onClick.AddListener(() => actionChoice = 0);
            if (bagButton)   bagButton.onClick.AddListener(()   => actionChoice = 1);
            if (partyButton) partyButton.onClick.AddListener(() => actionChoice = 2);
            if (runButton)   runButton.onClick.AddListener(()   => actionChoice = 3);

            for (int i = 0; i < moveButtons.Length; i++)
            {
                int idx = i;
                if (moveButtons[i] != null)
                    moveButtons[i].onClick.AddListener(() => moveChoice = idx);
            }
        }

        private void Update()
        {
            if (InputReader.Interact) continueRequested = true;
            // Mouse click anywhere advances messages too (fallback when Game view loses keyboard focus).
            if (Input.GetMouseButtonDown(0)) continueRequested = true;
        }

        private void HideAll()
        {
            if (battleRoot)   battleRoot.SetActive(false);
            if (messagePanel) messagePanel.SetActive(false);
            if (actionMenu)   actionMenu.SetActive(false);
            if (moveMenu)     moveMenu.SetActive(false);
        }

        private void Show(GameObject go, bool v) { if (go != null) go.SetActive(v); }

        public IEnumerator ShowIntro(PokemonInstance playerMon, PokemonInstance enemyMon, bool wild)
        {
            Show(battleRoot, true);
            BindPlayer(playerMon);
            BindEnemy(enemyMon);
            string intro = wild
                ? $"A wild {enemyMon.DisplayName} appeared!"
                : $"{enemyMon.DisplayName} wants to fight!";
            yield return ShowMessage(intro);
            yield return ShowMessage($"Go! {playerMon.DisplayName}!");
        }

        public IEnumerator ShowMessage(string text)
        {
            Show(messagePanel, true);
            Show(actionMenu, false);
            Show(moveMenu, false);

            if (messageLabel == null)
            {
                yield return new WaitForSeconds(0.5f);
                yield break;
            }

            messageLabel.text = "";
            continueRequested = false;
            float interval = 1f / Mathf.Max(1f, charsPerSecond);
            for (int i = 0; i < text.Length; i++)
            {
                if (continueRequested) { messageLabel.text = text; continueRequested = false; break; }
                messageLabel.text = text.Substring(0, i + 1);
                yield return new WaitForSeconds(interval);
            }
            messageLabel.text = text;

            // Wait for acknowledge
            while (!continueRequested) yield return null;
            continueRequested = false;
        }

        public IEnumerator ShowHPChange(PokemonInstance target, int before, int after)
        {
            bool isPlayer = target == GameManager.Instance?.PlayerParty?.Leader
                            || (playerHPBar != null && playerHPText != null && playerName != null && target.species != null
                                && playerName.text == target.DisplayName);
            Slider bar   = isPlayer ? playerHPBar  : enemyHPBar;
            TMP_Text txt = isPlayer ? playerHPText : null;

            if (bar != null)
            {
                float max = Mathf.Max(1, target.MaxHP);
                float duration = 0.35f;
                float t = 0f;
                float start = before / max;
                float end   = after  / max;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    bar.value = Mathf.Lerp(start, end, t / duration);
                    if (txt != null) txt.text = $"{Mathf.RoundToInt(Mathf.Lerp(before, after, t / duration))}/{target.MaxHP}";
                    yield return null;
                }
                bar.value = end;
                if (txt != null) txt.text = $"{after}/{target.MaxHP}";
            }
            else yield return null;
        }

        public IEnumerator ShowFaint(PokemonInstance target)
        {
            yield return ShowMessage($"{target.DisplayName} fainted!");
        }

        public IEnumerator AskPlayerAction(PokemonInstance active, Action<BattleAction> onChoice)
        {
            while (true)
            {
                Show(messagePanel, false);
                Show(actionMenu, true);
                Show(moveMenu, false);
                actionChoice = null;
                while (actionChoice == null) yield return null;

                switch (actionChoice.Value)
                {
                    case 0: // Fight
                        var moveAction = new BattleAction();
                        bool picked = false;
                        yield return PickMove(active, a => { moveAction = a; picked = true; });
                        if (picked) { onChoice(moveAction); yield break; }
                        break;
                    case 1: // Bag - not yet implemented
                        yield return ShowMessage("Bag is empty!");
                        break;
                    case 2: // Party - not yet implemented
                        yield return ShowMessage("Switching not implemented yet.");
                        break;
                    case 3: // Run
                        onChoice(new BattleAction { type = BattleActionType.Run });
                        yield break;
                }
            }
        }

        private IEnumerator PickMove(PokemonInstance active, Action<BattleAction> onChoice)
        {
            Show(actionMenu, false);
            Show(moveMenu, true);

            // Wire up labels
            for (int i = 0; i < moveButtons.Length; i++)
            {
                bool hasMove = i < active.moves.Count && active.moves[i].move != null;
                if (moveButtons[i] != null) moveButtons[i].interactable = hasMove && active.moves[i].currentPP > 0;
                if (moveLabels[i]  != null) moveLabels[i].text   = hasMove ? active.moves[i].move.moveName : "-";
                if (movePPLabels[i] != null)
                    movePPLabels[i].text = hasMove ? $"PP {active.moves[i].currentPP}/{active.moves[i].move.pp}" : "";
            }

            moveChoice = null;
            while (moveChoice == null)
            {
                if (InputReader.Cancel) { Show(moveMenu, false); yield break; }
                yield return null;
            }

            onChoice(new BattleAction { type = BattleActionType.Fight, moveIndex = moveChoice.Value });
        }

        private void BindPlayer(PokemonInstance p)
        {
            if (playerName  != null) playerName.text  = p.DisplayName;
            if (playerLevel != null) playerLevel.text = $"L{p.level}";
            if (playerHPText != null) playerHPText.text = $"{p.currentHP}/{p.MaxHP}";
            if (playerHPBar != null) playerHPBar.value = (float)p.currentHP / Mathf.Max(1, p.MaxHP);
            if (playerSprite != null && p.species != null) playerSprite.sprite = p.species.backSprite;
        }

        private void BindEnemy(PokemonInstance p)
        {
            if (enemyName  != null) enemyName.text  = p.DisplayName;
            if (enemyLevel != null) enemyLevel.text = $"L{p.level}";
            if (enemyHPBar != null) enemyHPBar.value = (float)p.currentHP / Mathf.Max(1, p.MaxHP);
            if (enemySprite != null && p.species != null) enemySprite.sprite = p.species.frontSprite;
        }
    }
}
