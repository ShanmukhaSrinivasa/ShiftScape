using System.Collections.Generic;

namespace ShiftScape.Core
{
    public static class PathFinder
    {
        // Two neighbouring tiles are connected only when both have an exit facing each other.
        public static List<GridPos> FindPath(Board board)
        {
            int width = board.Width;
            int cellCount = width * board.Height;
            var previous = new int[cellCount];
            for (int i = 0; i < cellCount; i++) previous[i] = -1;

            int start = board.Start.Y * width + board.Start.X;
            int goal = board.Goal.Y * width + board.Goal.X;
            var queue = new Queue<int>();
            previous[start] = start;
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                if (current == goal) break;

                var pos = new GridPos(current % width, current / width);
                Tile tile = board[pos];
                foreach (Direction d in DirectionExtensions.Cardinals)
                {
                    if (!tile.Connects(d)) continue;
                    GridPos next = pos.Neighbor(d);
                    if (!board.Contains(next)) continue;
                    if (!board[next].Connects(d.Opposite())) continue;
                    int nextIndex = next.Y * width + next.X;
                    if (previous[nextIndex] != -1) continue;
                    previous[nextIndex] = current;
                    queue.Enqueue(nextIndex);
                }
            }

            if (previous[goal] == -1) return null;

            var path = new List<GridPos>();
            for (int at = goal; at != start; at = previous[at])
                path.Add(new GridPos(at % width, at / width));
            path.Add(board.Start);
            path.Reverse();
            return path;
        }

        public static bool IsSolved(Board board) => FindPath(board) != null;
    }
}
