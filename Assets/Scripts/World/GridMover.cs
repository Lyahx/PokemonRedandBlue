using System.Collections;
using PokeRed.Core;
using UnityEngine;

namespace PokeRed.World
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class GridMover : MonoBehaviour
    {
        public const float TileSize = 1f;

        [SerializeField] private float moveTime = 0.16f;
        [SerializeField] private LayerMask solidLayer;

        public bool IsMoving { get; private set; }
        public Direction Facing { get; private set; } = Direction.Down;

        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            SnapToGrid();
        }

        public void SnapToGrid()
        {
            Vector3 p = transform.position;
            p.x = Mathf.Round(p.x / TileSize) * TileSize;
            p.y = Mathf.Round(p.y / TileSize) * TileSize;
            transform.position = p;
        }

        public bool TryMove(Direction dir)
        {
            if (IsMoving) return false;

            Facing = dir;
            Vector2 target = (Vector2)transform.position + (Vector2)dir.ToVector() * TileSize;

            if (IsBlocked(target)) return false;

            StartCoroutine(MoveRoutine(target));
            return true;
        }

        public void Face(Direction dir) => Facing = dir;

        private bool IsBlocked(Vector2 worldPos)
        {
            var hit = Physics2D.OverlapBox(worldPos, Vector2.one * 0.8f, 0f, solidLayer);
            return hit != null && hit.gameObject != gameObject;
        }

        private IEnumerator MoveRoutine(Vector2 target)
        {
            IsMoving = true;
            Vector2 start = transform.position;
            float t = 0f;
            while (t < moveTime)
            {
                t += Time.deltaTime;
                transform.position = Vector2.Lerp(start, target, t / moveTime);
                yield return null;
            }
            transform.position = target;
            IsMoving = false;
        }
    }
}
