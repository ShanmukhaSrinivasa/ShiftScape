using System.Collections.Generic;

namespace ShiftScape.Core
{
    public static class PathFinder
    {
        // Caps the crystal search so a pathological board can't stall a frame; real levels need far less.
        const int CrystalSearchBudget = 200000;

        // Two neighbouring tiles are connected only when both have an exit facing each other.
        // With crystals on the board, the path must be a simple path that passes through all of them.
        public static List<GridPos> FindPath(Board board)
        {
            int crystals = board.CrystalCount;
            return crystals == 0 ? ShortestPath(board) : PathThroughCrystals(board, crystals);
        }

        public static bool IsSolved(Board board) => FindPath(board) != null;

        static bool Connected(Board board, GridPos from, Direction d, out GridPos next)
        {
            next = from.Neighbor(d);
            return board[from].Connects(d) && board.Contains(next) && board[next].Connects(d.Opposite());
        }

        static List<GridPos> ShortestPath(Board board)
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
                foreach (Direction d in DirectionExtensions.Cardinals)
                {
                    if (!Connected(board, pos, d, out GridPos next)) continue;
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

        static List<GridPos> PathThroughCrystals(Board board, int totalCrystals)
        {
            var visited = new bool[board.Width * board.Height];
            var path = new List<GridPos>();
            int budget = CrystalSearchBudget;

            bool Search(GridPos at, int collected)
            {
                if (--budget < 0) return false;
                int index = at.Y * board.Width + at.X;
                visited[index] = true;
                path.Add(at);
                if (board[at].HasCrystal) collected++;

                // The walker stops at the goal, so it is only ever the last cell of the path.
                if (at.Equals(board.Goal))
                {
                    if (collected == totalCrystals) return true;
                }
                else
                {
                    foreach (Direction d in DirectionExtensions.Cardinals)
                    {
                        if (!Connected(board, at, d, out GridPos next)) continue;
                        if (visited[next.Y * board.Width + next.X]) continue;
                        if (Search(next, collected)) return true;
                    }
                }

                visited[index] = false;
                path.RemoveAt(path.Count - 1);
                return false;
            }

            return Search(board.Start, 0) ? path : null;
        }
    }
}
