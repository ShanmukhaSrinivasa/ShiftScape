namespace ShiftScape.Core
{
    public sealed class LevelData
    {
        public string Name { get; }
        public int Par { get; }
        public Board Board { get; }

        public LevelData(string name, int par, Board board)
        {
            Name = name;
            Par = par;
            Board = board;
        }
    }
}
