using System;
using System.Collections.Generic;

namespace ShiftScape.Core
{
    public sealed class PuzzleSession
    {
        readonly LevelData level;
        readonly Stack<ShiftMove> history = new Stack<ShiftMove>();

        public Board Board { get; private set; }
        public int MoveCount => history.Count;
        public int Par => level.Par;
        public string Name => level.Name;
        public bool IsSolved { get; private set; }
        public bool CanUndo => history.Count > 0 && !IsSolved;

        // Fired after the board changes; for an undo the move is the inverse that was applied.
        public event Action<ShiftMove> MoveApplied;
        public event Action Solved;
        public event Action Restarted;

        public PuzzleSession(LevelData level)
        {
            this.level = level;
            Board = level.Board.Clone();
            IsSolved = PathFinder.IsSolved(Board);
        }

        public bool TryShift(ShiftMove move)
        {
            if (IsSolved || !Board.TryShift(move)) return false;
            history.Push(move);
            MoveApplied?.Invoke(move);
            CheckSolved();
            return true;
        }

        public bool Undo()
        {
            if (!CanUndo) return false;
            ShiftMove inverse = history.Pop().Inverse;
            Board.TryShift(inverse);
            MoveApplied?.Invoke(inverse);
            return true;
        }

        public void Restart()
        {
            history.Clear();
            Board = level.Board.Clone();
            IsSolved = PathFinder.IsSolved(Board);
            Restarted?.Invoke();
        }

        void CheckSolved()
        {
            if (!PathFinder.IsSolved(Board)) return;
            IsSolved = true;
            Solved?.Invoke();
        }
    }
}
