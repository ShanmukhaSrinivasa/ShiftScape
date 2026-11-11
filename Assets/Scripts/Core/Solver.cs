using System.Collections.Generic;

namespace ShiftScape.Core
{
    // Breadth-first search, so the first solution found has the minimum number of moves.
    // Meant for validating levels and computing par, not for use at runtime.
    public static class Solver
    {
        sealed class Node
        {
            public Board Board;
            public Node Parent;
            public ShiftMove Move;
        }

        public static List<ShiftMove> Solve(Board start, int maxDepth = 8, int maxStates = 1500000)
        {
            if (PathFinder.IsSolved(start)) return new List<ShiftMove>();

            var visited = new HashSet<string> { start.StateKey() };
            var frontier = new List<Node> { new Node { Board = start.Clone() } };
            var moves = new List<ShiftMove>();

            for (int depth = 1; depth <= maxDepth; depth++)
            {
                var next = new List<Node>();
                foreach (Node node in frontier)
                {
                    node.Board.CollectLegalMoves(moves);
                    foreach (ShiftMove move in moves)
                    {
                        Board child = node.Board.Clone();
                        if (!child.TryShift(move)) continue;
                        if (!visited.Add(child.StateKey())) continue;

                        var childNode = new Node { Board = child, Parent = node, Move = move };
                        if (PathFinder.IsSolved(child)) return Reconstruct(childNode);
                        next.Add(childNode);
                        if (visited.Count > maxStates) return null;
                    }
                }
                frontier = next;
            }
            return null;
        }

        static List<ShiftMove> Reconstruct(Node last)
        {
            var result = new List<ShiftMove>();
            for (Node n = last; n.Parent != null; n = n.Parent) result.Add(n.Move);
            result.Reverse();
            return result;
        }
    }
}
