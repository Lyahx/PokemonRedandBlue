using System.Collections;
using PokeRed.Core;
using PokeRed.Pokemon;
using UnityEngine;

namespace PokeRed.Battle
{
    public class BattleSystem : MonoBehaviour
    {
        public static BattleSystem Instance { get; private set; }

        [SerializeField] private MonoBehaviour battleUiBehaviour; // must implement IBattleUI
        private IBattleUI ui;

        public BattlePhase Phase { get; private set; } = BattlePhase.Intro;

        private Party playerParty;
        private PokemonInstance activePlayer;
        private PokemonInstance activeEnemy;
        private bool trainerBattle;
        private BattleAction pendingAction;
        private bool hasAction;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            ui = battleUiBehaviour as IBattleUI;
        }

        public void StartWildBattle(Party party, PokemonInstance wild)
        {
            playerParty   = party;
            activeEnemy   = wild;
            trainerBattle = false;
            activePlayer  = party.FirstUsable();
            GameManager.Instance?.SetState(GameState.Battle);
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            Phase = BattlePhase.Intro;
            if (ui != null) yield return ui.ShowIntro(activePlayer, activeEnemy, !trainerBattle);

            while (true)
            {
                Phase = BattlePhase.WaitingForPlayerAction;
                hasAction = false;
                if (ui != null) yield return ui.AskPlayerAction(activePlayer, a => { pendingAction = a; hasAction = true; });
                while (!hasAction) yield return null;

                if (pendingAction.type == BattleActionType.Run)
                {
                    if (ui != null) yield return ui.ShowMessage("Got away safely!");
                    Phase = BattlePhase.Escaped;
                    break;
                }

                yield return ResolveTurn(pendingAction);

                if (activeEnemy.IsFainted)
                {
                    yield return HandleEnemyFaint();
                    Phase = BattlePhase.Victory;
                    break;
                }
                if (activePlayer.IsFainted)
                {
                    yield return HandlePlayerFaint();
                    if (!playerParty.HasUsable) { Phase = BattlePhase.Defeat; break; }
                    activePlayer = playerParty.FirstUsable();
                    if (ui != null) yield return ui.ShowMessage($"Go! {activePlayer.DisplayName}!");
                }
            }

            EndBattle();
        }

        private IEnumerator ResolveTurn(BattleAction playerAction)
        {
            Phase = BattlePhase.ResolvingTurn;
            var enemyAction = ChooseEnemyAction();

            bool playerFirst = DeterminePlayerFirst(playerAction, enemyAction);

            if (playerFirst)
            {
                yield return PerformMove(activePlayer, activeEnemy, MoveFromAction(activePlayer, playerAction));
                if (!activeEnemy.IsFainted)
                    yield return PerformMove(activeEnemy, activePlayer, MoveFromAction(activeEnemy, enemyAction));
            }
            else
            {
                yield return PerformMove(activeEnemy, activePlayer, MoveFromAction(activeEnemy, enemyAction));
                if (!activePlayer.IsFainted)
                    yield return PerformMove(activePlayer, activeEnemy, MoveFromAction(activePlayer, playerAction));
            }
        }

        private bool DeterminePlayerFirst(BattleAction pa, BattleAction ea)
        {
            int pPrio = (pa.type == BattleActionType.Fight && activePlayer.moves.Count > pa.moveIndex)
                ? activePlayer.moves[pa.moveIndex].move.priority : 0;
            int ePrio = (ea.type == BattleActionType.Fight && activeEnemy.moves.Count > ea.moveIndex)
                ? activeEnemy.moves[ea.moveIndex].move.priority : 0;
            if (pPrio != ePrio) return pPrio > ePrio;
            if (activePlayer.Speed != activeEnemy.Speed) return activePlayer.Speed > activeEnemy.Speed;
            return Random.value < 0.5f;
        }

        private MoveData MoveFromAction(PokemonInstance owner, BattleAction a)
        {
            if (a.type != BattleActionType.Fight) return null;
            if (a.moveIndex < 0 || a.moveIndex >= owner.moves.Count) return null;
            var slot = owner.moves[a.moveIndex];
            return slot.currentPP > 0 ? slot.move : null;
        }

        private IEnumerator PerformMove(PokemonInstance attacker, PokemonInstance defender, MoveData move)
        {
            if (move == null)
            {
                if (ui != null) yield return ui.ShowMessage($"{attacker.DisplayName} has no usable moves!");
                yield break;
            }

            // decrement PP on the slot
            foreach (var s in attacker.moves) if (s.move == move) { s.currentPP = Mathf.Max(0, s.currentPP - 1); break; }

            if (ui != null) yield return ui.ShowMessage($"{attacker.DisplayName} used {move.moveName}!");

            var result = DamageCalc.Compute(move, attacker, defender);

            if (result.missed)
            {
                if (ui != null) yield return ui.ShowMessage($"{attacker.DisplayName}'s attack missed!");
                yield break;
            }

            int before = defender.currentHP;
            defender.TakeDamage(result.damage);
            if (ui != null) yield return ui.ShowHPChange(defender, before, defender.currentHP);

            if (result.criticalHit && result.damage > 0 && ui != null)
                yield return ui.ShowMessage("A critical hit!");
            if (result.effectiveness > 1f && ui != null)
                yield return ui.ShowMessage("It's super effective!");
            else if (result.effectiveness > 0f && result.effectiveness < 1f && ui != null)
                yield return ui.ShowMessage("It's not very effective...");
            else if (result.effectiveness == 0f && ui != null)
                yield return ui.ShowMessage("It had no effect.");

            if (defender.IsFainted && ui != null)
                yield return ui.ShowFaint(defender);
        }

        private BattleAction ChooseEnemyAction()
        {
            // Simple AI: pick a random move with PP > 0
            int tries = 0;
            while (tries++ < 8 && activeEnemy.moves.Count > 0)
            {
                int i = Random.Range(0, activeEnemy.moves.Count);
                if (activeEnemy.moves[i].currentPP > 0)
                    return new BattleAction { type = BattleActionType.Fight, moveIndex = i };
            }
            return new BattleAction { type = BattleActionType.Fight, moveIndex = 0 };
        }

        private IEnumerator HandleEnemyFaint()
        {
            int exp = DamageCalc.ExpGain(activeEnemy, trainerBattle);
            if (ui != null) yield return ui.ShowMessage($"{activePlayer.DisplayName} gained {exp} EXP!");
            if (activePlayer.GainExp(exp, out int levels) && ui != null)
                yield return ui.ShowMessage($"{activePlayer.DisplayName} grew to L{activePlayer.level}!");
        }

        private IEnumerator HandlePlayerFaint()
        {
            if (ui != null) yield return ui.ShowMessage($"{activePlayer.DisplayName} fainted!");
        }

        private void EndBattle()
        {
            GameManager.Instance?.SetState(GameState.Overworld);
        }
    }
}
