using System;

namespace ShiftScape.Core
{
    public readonly struct Tile : IEquatable<Tile>
    {
        public readonly Direction Exits;
        public readonly bool IsFixed;
        // A crystal rides along with its tile; the solving path must pass through every one.
        public readonly bool HasCrystal;

        public Tile(Direction exits, bool isFixed = false, bool hasCrystal = false)
        {
            Exits = exits;
            IsFixed = isFixed;
            HasCrystal = hasCrystal;
        }

        public bool IsEmpty => Exits == Direction.None;
        public bool Connects(Direction d) => Exits.Has(d);
        public Tile WithoutCrystal => new Tile(Exits, IsFixed);

        public bool Equals(Tile other) => Exits == other.Exits && IsFixed == other.IsFixed && HasCrystal == other.HasCrystal;
        public override bool Equals(object obj) => obj is Tile other && Equals(other);
        public override int GetHashCode() => ((int)Exits << 2) | (IsFixed ? 1 : 0) | (HasCrystal ? 2 : 0);
    }
}
