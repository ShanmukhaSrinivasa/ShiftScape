namespace ShiftScape.Core
{
    // Step +1 slides a row to the right or a column down; -1 is the opposite.
    public readonly struct ShiftMove
    {
        public readonly bool IsRow;
        public readonly int Index;
        public readonly int Step;

        public ShiftMove(bool isRow, int index, int step)
        {
            IsRow = isRow;
            Index = index;
            Step = step;
        }

        public static ShiftMove Row(int index, int step) => new ShiftMove(true, index, step);
        public static ShiftMove Column(int index, int step) => new ShiftMove(false, index, step);

        public ShiftMove Inverse => new ShiftMove(IsRow, Index, -Step);

        public override string ToString() =>
            IsRow ? $"Row {Index} {(Step > 0 ? "right" : "left")}" : $"Column {Index} {(Step > 0 ? "down" : "up")}";
    }
}
