using System.Collections.Generic;
using PokeRed.Core;
using PokeRed.Dialogue;
using PokeRed.World;
using UnityEngine;

namespace PokeRed.NPC
{
    [RequireComponent(typeof(GridMover))]
    public class NPCController : MonoBehaviour, IInteractable
    {
        [SerializeField] private Direction startFacing = Direction.Down;
        [TextArea] [SerializeField] private List<string> lines = new();

        [Header("Optional wander")]
        [SerializeField] private bool wander = false;
        [SerializeField] private float wanderInterval = 3f;
        [SerializeField] private int wanderRadius = 2;

        private GridMover mover;
        private Vector2 origin;
        private float nextWanderTime;

        private void Awake()
        {
            mover = GetComponent<GridMover>();
            mover.Face(startFacing);
            origin = transform.position;
            nextWanderTime = Time.time + wanderInterval;
        }

        private void Update()
        {
            if (!wander) return;
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Overworld) return;
            if (mover.IsMoving || Time.time < nextWanderTime) return;

            var dir = (Direction)Random.Range(0, 4);
            Vector2 delta = ((Vector2)transform.position + (Vector2)dir.ToVector()) - origin;
            if (Mathf.Abs(delta.x) > wanderRadius || Mathf.Abs(delta.y) > wanderRadius) return;

            mover.TryMove(dir);
            nextWanderTime = Time.time + wanderInterval;
        }

        public void Interact(Direction from)
        {
            mover.Face(from);
            if (lines.Count > 0)
                DialogueManager.Instance?.Show(lines);
        }
    }
}
