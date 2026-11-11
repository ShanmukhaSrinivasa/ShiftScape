using System.Collections.Generic;
using System.Text;

namespace ShiftScape.Core
{
    public sealed class Board
    {
        readonly Tile[] tiles;
        readonly bool[] lockedRows;
        readonly bool[] lockedColumns;
        readonly int[] scratchIndices;
        readonly Tile[] scratchTiles;

        public int Width { get; }
        public int Height { get; }
        public GridPos Start { get; }
        public GridPos Goal { get; }

        public Board(int width, int height, GridPos start, GridPos goal,
            IEnumerable<int> lockedRowIndices = null, IEnumerable<int> lockedColumnIndices = null)
            : this(width, height, start, goal, new Tile[width * height],
                ToFlags(lockedRowIndices, height), ToFlags(lockedColumnIndices, width))
        {
        }

        Board(int width, int height, GridPos start, GridPos goal, Tile[] tiles, bool[] lockedRows, bool[] lockedColumns)
        {
            Width = width;
            Height = height;
            Start = start;
            Goal = goal;
            this.tiles = tiles;
            this.lockedRows = lockedRows;
            this.lockedColumns = lockedColumns;
            int longest = width > height ? width : height;
            scratchIndices = new int[longest];
            scratchTiles = new Tile[longest];
        }

        static bool[] ToFlags(IEnumerable<int> indices, int size)
        {
            var flags = new bool[size];
            if (indices == null) return flags;
            foreach (int i in indices)
                if (i >= 0 && i < size) flags[i] = true;
            return flags;
        }

        public Tile this[int x, int y] => tiles[y * Width + x];
        public Tile this[GridPos p] => tiles[p.Y * Width + p.X];

        public void SetTile(int x, int y, Tile tile) => tiles[y * Width + x] = tile;

        public bool Contains(GridPos p) => p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height;

        public bool IsRowLocked(int row) => lockedRows[row];
        public bool IsColumnLocked(int column) => lockedColumns[column];

        public bool CanShift(ShiftMove move)
        {
            if (move.Step != 1 && move.Step != -1) return false;
            if (move.IsRow)
                return move.Index >= 0 && move.Index < Height && !lockedRows[move.Index];
            return move.Index >= 0 && move.Index < Width && !lockedColumns[move.Index];
        }

        // Fixed tiles stay put; the movable tiles of the line cycle among the movable cells.
        // Returns false (and leaves the board untouched) when the move would change nothing.
        public bool TryShift(ShiftMove move)
        {
            if (!CanShift(move)) return false;

            int length = move.IsRow ? Width : Height;
            int movable = 0;
            for (int i = 0; i < length; i++)
            {
                int cell = move.IsRow ? move.Index * Width + i : i * Width + move.Index;
                if (tiles[cell].IsFixed) continue;
                scratchIndices[movable] = cell;
                scratchTiles[movable] = tiles[cell];
                movable++;
            }
            if (movable < 2) return false;

            bool changed = false;
            for (int k = 0; k < movable; k++)
            {
                int dest = ((k + move.Step) % movable + movable) % movable;
                if (!scratchTiles[dest].Equals(scratchTiles[k])) changed = true;
                tiles[scratchIndices[dest]] = scratchTiles[k];
            }
            return changed;
        }

        public void CollectLegalMoves(List<ShiftMove> into)
        {
            into.Clear();
            for (int row = 0; row < Height; row++)
            {
                if (lockedRows[row]) continue;
                into.Add(ShiftMove.Row(row, -1));
                into.Add(ShiftMove.Row(row, 1));
            }
            for (int column = 0; column < Width; column++)
            {
                if (lockedColumns[column]) continue;
                into.Add(ShiftMove.Column(column, -1));
                into.Add(ShiftMove.Column(column, 1));
            }
        }

        public Board Clone() =>
            new Board(Width, Height, Start, Goal, (Tile[])tiles.Clone(), lockedRows, lockedColumns);

        public string StateKey()
        {
            var sb = new StringBuilder(tiles.Length);
            foreach (Tile t in tiles)
                sb.Append((char)('A' + (int)t.Exits + (t.IsFixed ? 16 : 0)));
            return sb.ToString();
        }
    }
}
