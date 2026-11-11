using System;

namespace ShiftScape.Core
{
    // X grows to the right, Y grows downward (row 0 is the top row of the level text).
    public readonly struct GridPos : IEquatable<GridPos>
    {
        public readonly int X;
        public readonly int Y;

        public GridPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public GridPos Neighbor(Direction d)
        {
            switch (d)
            {
                case Direction.Up: return new GridPos(X, Y - 1);
                case Direction.Down: return new GridPos(X, Y + 1);
                case Direction.Left: return new GridPos(X - 1, Y);
                case Direction.Right: return new GridPos(X + 1, Y);
                default: return this;
            }
        }

        public bool Equals(GridPos other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridPos other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public override string ToString() => $"({X},{Y})";
    }
}
