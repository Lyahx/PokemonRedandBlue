using PokeRed.Core;
using PokeRed.NPC;
using PokeRed.World;
using UnityEngine;

namespace PokeRed.Player
{
    [RequireComponent(typeof(GridMover))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float interactRange = 1f;
        [SerializeField] private LayerMask interactLayer;

        private GridMover mover;

        private void Awake() => mover = GetComponent<GridMover>();

        private void Update()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.State != GameState.Overworld) return;

            if (!mover.IsMoving && InputReader.TryReadDirection(out var dir))
            {
                if (!mover.TryMove(dir)) mover.Face(dir);
            }

            if (InputReader.Interact && !mover.IsMoving)
                TryInteract();
        }

        private void TryInteract()
        {
            Vector2 origin = (Vector2)transform.position + (Vector2)mover.Facing.ToVector() * interactRange;
            var hit = Physics2D.OverlapBox(origin, Vector2.one * 0.8f, 0f, interactLayer);
            if (hit == null) return;

            if (hit.TryGetComponent<IInteractable>(out var target))
                target.Interact(mover.Facing.Opposite());
        }
    }
}
