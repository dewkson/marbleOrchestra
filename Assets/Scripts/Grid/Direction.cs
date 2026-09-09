using System;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    [Flags]
    public enum Direction
    {
        None = 0,
        Up = 1 << 0,
        Right = 1 << 1,
        Down = 1 << 2,
        Left = 1 << 3
    }

    public static class DirectionExtensions
    {
        public static readonly Direction[] All = { Direction.Up, Direction.Right, Direction.Down, Direction.Left };

        public static Direction Opposite(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return Direction.Down;
                case Direction.Down: return Direction.Up;
                case Direction.Left: return Direction.Right;
                case Direction.Right: return Direction.Left;
                default: return Direction.None;
            }
        }

        public static Vector2Int ToGridOffset(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return new Vector2Int(0, 1);
                case Direction.Down: return new Vector2Int(0, -1);
                case Direction.Left: return new Vector2Int(-1, 0);
                case Direction.Right: return new Vector2Int(1, 0);
                default: return Vector2Int.zero;
            }
        }

        /// Same axis mapping as ToGridOffset (grid Y -> local Z), just as a
        /// horizontal unit Vector3 - used by TrackBlock's curved-groove
        /// geometry (see 0040) to work directly in grid-axis-aligned local
        /// coordinates instead of a yaw-rotated frame.
        public static Vector3 ToLocalVector3(this Direction direction)
        {
            Vector2Int offset = direction.ToGridOffset();
            return new Vector3(offset.x, 0f, offset.y);
        }
    }
}
