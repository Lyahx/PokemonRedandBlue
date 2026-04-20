using UnityEngine;

namespace PokeRed.World
{
    public class SpawnPoint : MonoBehaviour
    {
        public static string PendingSpawnId;

        [SerializeField] private string id = "Default";
        [SerializeField] private Transform player;

        private void Start()
        {
            if (!string.IsNullOrEmpty(PendingSpawnId) && PendingSpawnId == id && player != null)
            {
                player.position = transform.position;
                if (player.TryGetComponent<GridMover>(out var mover)) mover.SnapToGrid();
                PendingSpawnId = null;
            }
        }
    }
}
