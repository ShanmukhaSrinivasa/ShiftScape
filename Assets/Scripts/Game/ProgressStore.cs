using ShiftScape.Core;
using UnityEngine;

namespace ShiftScape.Game
{
    // Keyed by level name so reordering levels doesn't scramble saved progress.
    public static class ProgressStore
    {
        const string BestPrefix = "shiftscape.best.";

        public static int BestMoves(LevelData level) => PlayerPrefs.GetInt(BestPrefix + level.Name, 0);

        public static bool IsCompleted(LevelData level) => BestMoves(level) > 0;

        public static int Stars(LevelData level) =>
            IsCompleted(level) ? StarRating.For(BestMoves(level), level.Par) : 0;

        // Testing aid only; toggled from the keyboard in the editor and development builds.
        public static bool UnlockAll { get; set; }

        public static bool IsUnlocked(int index) =>
            UnlockAll || index == 0 || IsCompleted(LevelLibrary.Levels[index - 1]);

        public static void RecordWin(LevelData level, int moves)
        {
            int best = BestMoves(level);
            if (best != 0 && moves >= best) return;
            PlayerPrefs.SetInt(BestPrefix + level.Name, moves);
            PlayerPrefs.Save();
        }

        public static int FirstUnfinishedIndex()
        {
            var levels = LevelLibrary.Levels;
            for (int i = 0; i < levels.Count; i++)
                if (!IsCompleted(levels[i])) return i;
            return 0;
        }

        public static int TotalStars()
        {
            int total = 0;
            foreach (LevelData level in LevelLibrary.Levels) total += Stars(level);
            return total;
        }
    }
}
