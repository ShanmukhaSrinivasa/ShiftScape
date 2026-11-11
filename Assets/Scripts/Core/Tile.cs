using System;

namespace ShiftScape.Core
{
    public readonly struct Tile : IEquatable<Tile>
    {
        public readonly Direction Exits;
        public readonly bool IsFixed;

        public Tile(Direction exits, bool isFixed = false)
        {
            Exits = exits;
            IsFixed = isFixed;
        }

        public bool IsEmpty => Exits == Direction.None;
        public bool Connects(Direction d) => Exits.Has(d);

        public bool Equals(Tile other) => Exits == other.Exits && IsFixed == other.IsFixed;
        public override bool Equals(object obj) => obj is Tile other && Equals(other);
        public override int GetHashCode() => ((int)Exits << 1) | (IsFixed ? 1 : 0);
    }
}
