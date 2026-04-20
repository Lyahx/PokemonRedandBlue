using UnityEngine;

namespace PokeRed.Core
{
    public static class InputReader
    {
        public static bool TryReadDirection(out Direction dir)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(v) >= Mathf.Abs(h))
            {
                if (v >  0.1f) { dir = Direction.Up;   return true; }
                if (v < -0.1f) { dir = Direction.Down; return true; }
            }
            if (h >  0.1f) { dir = Direction.Right; return true; }
            if (h < -0.1f) { dir = Direction.Left;  return true; }

            dir = Direction.Down;
            return false;
        }

        public static bool Interact   => Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E);
        public static bool Cancel     => Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Backspace);
        public static bool OpenMenu   => Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab);
    }
}
