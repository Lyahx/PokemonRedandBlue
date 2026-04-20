using UnityEngine;
using UnityEngine.SceneManagement;

namespace PokeRed.World
{
    [RequireComponent(typeof(Collider2D))]
    public class Warp : MonoBehaviour
    {
        [SerializeField] private string targetScene;
        [SerializeField] private string targetSpawnId = "Default";
        [SerializeField] private bool sameScene = false;
        [SerializeField] private Vector2 targetPosition;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            if (sameScene)
            {
                other.transform.position = targetPosition;
                if (other.TryGetComponent<GridMover>(out var mover)) mover.SnapToGrid();
                return;
            }

            SpawnPoint.PendingSpawnId = targetSpawnId;
            SceneManager.LoadScene(targetScene);
        }
    }
}
