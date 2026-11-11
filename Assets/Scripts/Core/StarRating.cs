namespace ShiftScape.Core
{
    public static class StarRating
    {
        public const int MaxStars = 3;

        // 3 stars at or under par, 2 stars within roughly double par, otherwise 1.
        public static int For(int moves, int par)
        {
            if (moves <= par) return 3;
            if (moves <= par * 2 + 1) return 2;
            return 1;
        }
    }
}
