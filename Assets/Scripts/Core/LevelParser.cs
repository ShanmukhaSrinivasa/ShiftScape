using System;
using System.Collections.Generic;
using System.Text;

namespace ShiftScape.Core
{
    // Level text format:
    //   name: First Steps
    //   par: 1
    //   start: 0,1            (x,y; y = 0 is the top row)
    //   goal: 2,1
    //   lockedRows: 1         (optional, comma separated)
    //   lockedColumns: 0,2    (optional)
    //   hint: Drag a column   (optional, shown in the HUD)
    //   grid:
    //   .  .  L
    //   R  RL+ .
    //   .  .  .
    // Each grid token lists a tile's exits (U, R, D, L), "." is empty, a trailing "+" puts a crystal
    // on the tile and a trailing "*" marks a fixed tile (both may be combined, e.g. "UD+*").
    public static class LevelParser
    {
        public static LevelData Parse(string text)
        {
            string name = "Untitled";
            string hint = "";
            int par = 0;
            GridPos? start = null, goal = null;
            var lockedRows = new List<int>();
            var lockedColumns = new List<int>();
            var gridRows = new List<string[]>();
            bool inGrid = false;

            string[] lines = text.Replace("\r", "").Split('\n');
            for (int lineNumber = 1; lineNumber <= lines.Length; lineNumber++)
            {
                string line = lines[lineNumber - 1].Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                if (inGrid)
                {
                    gridRows.Add(line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
                    continue;
                }

                int colon = line.IndexOf(':');
                if (colon < 0) throw Error(lineNumber, $"expected 'key: value', got '{line}'");
                string key = line.Substring(0, colon).Trim().ToLowerInvariant();
                string value = line.Substring(colon + 1).Trim();

                switch (key)
                {
                    case "name": name = value; break;
                    case "hint": hint = value; break;
                    case "par": par = ParseInt(value, lineNumber); break;
                    case "start": start = ParsePos(value, lineNumber); break;
                    case "goal": goal = ParsePos(value, lineNumber); break;
                    case "lockedrows": lockedRows = ParseIntList(value, lineNumber); break;
                    case "lockedcolumns": lockedColumns = ParseIntList(value, lineNumber); break;
                    case "grid": inGrid = true; break;
                    default: throw Error(lineNumber, $"unknown key '{key}'");
                }
            }

            if (gridRows.Count == 0) throw new FormatException($"Level '{name}': missing grid.");
            if (start == null || goal == null) throw new FormatException($"Level '{name}': start and goal are required.");

            int width = gridRows[0].Length;
            int height = gridRows.Count;
            foreach (string[] row in gridRows)
                if (row.Length != width) throw new FormatException($"Level '{name}': grid rows have different lengths.");

            var board = new Board(width, height, start.Value, goal.Value, lockedRows, lockedColumns);
            if (!board.Contains(start.Value) || !board.Contains(goal.Value))
                throw new FormatException($"Level '{name}': start or goal is outside the grid.");
            foreach (int r in lockedRows)
                if (r < 0 || r >= height) throw new FormatException($"Level '{name}': locked row {r} is outside the grid.");
            foreach (int c in lockedColumns)
                if (c < 0 || c >= width) throw new FormatException($"Level '{name}': locked column {c} is outside the grid.");

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                board.SetTile(x, y, ParseTile(gridRows[y][x], name));

            return new LevelData(name, par, board, hint);
        }

        public static string Serialize(LevelData level)
        {
            Board b = level.Board;
            var sb = new StringBuilder();
            sb.AppendLine($"name: {level.Name}");
            sb.AppendLine($"par: {level.Par}");
            sb.AppendLine($"start: {b.Start.X},{b.Start.Y}");
            sb.AppendLine($"goal: {b.Goal.X},{b.Goal.Y}");
            if (level.Hint.Length > 0) sb.AppendLine($"hint: {level.Hint}");

            var rows = new List<string>();
            for (int y = 0; y < b.Height; y++) if (b.IsRowLocked(y)) rows.Add(y.ToString());
            if (rows.Count > 0) sb.AppendLine($"lockedRows: {string.Join(",", rows)}");
            var columns = new List<string>();
            for (int x = 0; x < b.Width; x++) if (b.IsColumnLocked(x)) columns.Add(x.ToString());
            if (columns.Count > 0) sb.AppendLine($"lockedColumns: {string.Join(",", columns)}");

            sb.AppendLine("grid:");
            for (int y = 0; y < b.Height; y++)
            {
                var tokens = new string[b.Width];
                for (int x = 0; x < b.Width; x++)
                {
                    Tile t = b[x, y];
                    tokens[x] = (t.Exits.ToLetters() + (t.HasCrystal ? "+" : "") + (t.IsFixed ? "*" : "")).PadRight(6);
                }
                sb.AppendLine(string.Join("", tokens).TrimEnd());
            }
            return sb.ToString();
        }

        static Tile ParseTile(string token, string levelName)
        {
            bool isFixed = false, hasCrystal = false;
            string letters = token;
            while (letters.Length > 1 && (letters.EndsWith("*") || letters.EndsWith("+")))
            {
                if (letters.EndsWith("*")) isFixed = true;
                else hasCrystal = true;
                letters = letters.Substring(0, letters.Length - 1);
            }
            if (letters == ".") return new Tile(Direction.None, isFixed, hasCrystal);
            if (!DirectionExtensions.TryParseLetters(letters, out Direction exits))
                throw new FormatException($"Level '{levelName}': bad tile '{token}'.");
            return new Tile(exits, isFixed, hasCrystal);
        }

        static int ParseInt(string value, int lineNumber)
        {
            if (!int.TryParse(value, out int result)) throw Error(lineNumber, $"'{value}' is not a number");
            return result;
        }

        static List<int> ParseIntList(string value, int lineNumber)
        {
            var list = new List<int>();
            foreach (string part in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                list.Add(ParseInt(part.Trim(), lineNumber));
            return list;
        }

        static GridPos ParsePos(string value, int lineNumber)
        {
            List<int> parts = ParseIntList(value, lineNumber);
            if (parts.Count != 2) throw Error(lineNumber, $"expected 'x,y', got '{value}'");
            return new GridPos(parts[0], parts[1]);
        }

        static FormatException Error(int lineNumber, string message) =>
            new FormatException($"Level text line {lineNumber}: {message}.");
    }
}
