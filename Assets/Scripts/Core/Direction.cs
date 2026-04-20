using UnityEngine;

namespace PokeRed.Core
{
    public enum Direction { Down, Up, Left, Right }

    public static class DirectionExtensions
    {
        public static Vector2Int ToVector(this Direction d) => d switch
        {
            Direction.Down  => new Vector2Int(0, -1),
            Direction.Up    => new Vector2Int(0,  1),
            Direction.Left  => new Vector2Int(-1, 0),
            Direction.Right => new Vector2Int( 1, 0),
            _ => Vector2Int.zero
        };

        public static Direction Opposite(this Direction d) => d switch
        {
            Direction.Down  => Direction.Up,
            Direction.Up    => Direction.Down,
            Direction.Left  => Direction.Right,
            Direction.Right => Direction.Left,
            _ => d
        };
    }
}
