using System;

namespace ShiftScape.Core
{
    [Flags]
    public enum Direction : byte
    {
        None = 0,
        Up = 1,
        Right = 2,
        Down = 4,
        Left = 8,
    }

    public static class DirectionExtensions
    {
        public static readonly Direction[] Cardinals =
        {
            Direction.Up, Direction.Right, Direction.Down, Direction.Left,
        };

        public static bool Has(this Direction mask, Direction d) => (mask & d) != 0;

        public static Direction Opposite(this Direction d)
        {
            switch (d)
            {
                case Direction.Up: return Direction.Down;
                case Direction.Down: return Direction.Up;
                case Direction.Left: return Direction.Right;
                case Direction.Right: return Direction.Left;
                default: return Direction.None;
            }
        }

        public static bool TryParseLetters(string letters, out Direction mask)
        {
            mask = Direction.None;
            foreach (char c in letters)
            {
                switch (char.ToUpperInvariant(c))
                {
                    case 'U': mask |= Direction.Up; break;
                    case 'R': mask |= Direction.Right; break;
                    case 'D': mask |= Direction.Down; break;
                    case 'L': mask |= Direction.Left; break;
                    default: return false;
                }
            }
            return true;
        }

        public static string ToLetters(this Direction mask)
        {
            if (mask == Direction.None) return ".";
            string s = "";
            if (mask.Has(Direction.Up)) s += "U";
            if (mask.Has(Direction.Right)) s += "R";
            if (mask.Has(Direction.Down)) s += "D";
            if (mask.Has(Direction.Left)) s += "L";
            return s;
        }
    }
}
