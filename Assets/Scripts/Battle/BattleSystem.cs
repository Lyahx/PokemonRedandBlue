using System.Collections;
using System.Collections.Generic;
using PokeRed.Core;
using PokeRed.Items;
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
        private TrainerData trainer;
        private List<PokemonInstance> trainerParty;
        private int trainerPartyIndex;
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
            trainer       = null;
            trainerParty  = null;
            activePlayer  = party.FirstUsable();
            GameManager.Instance?.SetState(GameState.Battle);
            StartCoroutine(Run());
        }

        public void StartTrainerBattle(Party party, TrainerData trainerData)
        {
            playerParty      = party;
            trainer          = trainerData;
            trainerParty     = trainerData.BuildParty();
            if (trainerParty == null || trainerParty.Count == 0) return;
            trainerPartyIndex = 0;
            activeEnemy       = trainerParty[0];
            trainerBattle     = true;
            activePlayer      = party.FirstUsable();
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
                    if (trainerBattle)
                    {
                        if (ui != null) yield return ui.ShowMessage("No running from a trainer battle!");
                        continue;
                    }
                    if (ui != null) yield return ui.ShowMessage("Got away safely!");
                    Phase = BattlePhase.Escaped;
                    break;
                }

                if (pendingAction.type == BattleActionType.UseItem && pendingAction.item != null)
                {
                    bool captured = false;
                    yield return UseItemAction(pendingAction.item, used => captured = used);
                    if (captured) { Phase = BattlePhase.Captured; break; }
                    // Enemy still acts after item use
                    if (activeEnemy != null && !activeEnemy.IsFainted)
                    {
                        var ea = ChooseEnemyAction();
                        yield return PerformMove(activeEnemy, activePlayer, MoveFromAction(activeEnemy, ea));
                    }
                }
                else
                {
                    yield return ResolveTurn(pendingAction);
                }

                if (activeEnemy != null && activeEnemy.IsFainted)
                {
                    yield return HandleEnemyFaint();

                    if (trainerBattle && TryAdvanceTrainer())
                    {
                        if (ui != null) yield return ui.ShowMessage($"{trainer.trainerName} sent out {activeEnemy.DisplayName}!");
                        continue;
                    }

                    Phase = BattlePhase.Victory;
                    if (trainerBattle && trainer != null)
                    {
                        int reward = trainer.RewardMoney();
                        if (GameManager.Instance != null) GameManager.Instance.Money += reward;
                        if (ui != null) yield return ui.ShowMessage($"You got ₽{reward} for winning!");
                    }
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

            if (ui != null) yield return ui.HideBattle();
            EndBattle();
        }

        private bool TryAdvanceTrainer()
        {
            if (trainerParty == null) return false;
            trainerPartyIndex++;
            while (trainerPartyIndex < trainerParty.Count)
            {
                if (!trainerParty[trainerPartyIndex].IsFainted)
                {
                    activeEnemy = trainerParty[trainerPartyIndex];
                    return true;
                }
                trainerPartyIndex++;
            }
            return false;
        }

        private IEnumerator UseItemAction(ItemData item, System.Action<bool> onCaptured)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.Bag.Consume(item, 1);

            if (ui != null) yield return ui.ShowMessage($"Used {item.itemName}.");

            if (item.category == ItemCategory.Ball)
            {
                if (trainerBattle)
                {
                    if (ui != null) yield return ui.ShowMessage("You can't catch a trainer's Pokémon!");
                    onCaptured(false); yield break;
                }
                bool caught = CatchCalculator.TryCatch(activeEnemy, item.ballModifier, out int shakes);
                if (ui != null) yield return ui.ShowMessage(ShakeMessage(shakes, caught));
                if (caught)
                {
                    playerParty.Add(activeEnemy);
                    onCaptured(true); yield break;
                }
            }
            else if (item.category == ItemCategory.Potion && item.healAmount > 0)
            {
                int before = activePlayer.currentHP;
                activePlayer.Heal(item.healAmount);
                if (ui != null)
                {
                    yield return ui.ShowHPChange(activePlayer, before, activePlayer.currentHP);
                    yield return ui.ShowMessage($"{activePlayer.DisplayName} restored {activePlayer.currentHP - before} HP.");
                }
            }
            onCaptured(false);
        }

        private static string ShakeMessage(int shakes, bool caught)
        {
            if (caught) return "Gotcha! It was caught!";
            return shakes switch
            {
                0 => "Oh no! It broke free!",
                1 => "Aww! It appeared to be caught!",
                2 => "Aargh! Almost had it!",
                _ => "Shoot! It was so close too!"
            };
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
