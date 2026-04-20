using PokeRed.Battle;
using PokeRed.Core;
using UnityEngine;

namespace PokeRed.World
{
    [RequireComponent(typeof(Collider2D))]
    public class EncounterZone : MonoBehaviour
    {
        [SerializeField] private EncounterTable table;

        private Vector2 lastCheckPos;
        private bool playerInside;
        private Transform player;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            playerInside = true;
            player = other.transform;
            lastCheckPos = player.position;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            playerInside = false;
            player = null;
        }

        private void Update()
        {
            if (!playerInside || player == null) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Overworld) return;

            Vector2 pos = player.position;
            if (Vector2.Distance(pos, lastCheckPos) < GridMover.TileSize * 0.9f) return;
            lastCheckPos = pos;

            if (Random.value > table.stepEncounterChance) return;

            var wild = table.RollEncounter();
            if (wild == null) return;

            var party = GameManager.Instance.PlayerParty;
            if (party == null || !party.HasUsable) return;

            BattleSystem.Instance?.StartWildBattle(party, wild);
        }
    }
}
