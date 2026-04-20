using UnityEngine;

namespace PokeRed.Core
{
    /// <summary>Smoothly follows a target on the XY plane, keeping its own Z.</summary>
    public class FollowTarget : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.08f;

        private Vector3 velocity;

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 goal = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
        }
    }
}
