namespace NLDB.DAL
{
    public struct AValue
    {
        public int Rank { get; set; }

        public int R { get; set; }

        public int C { get; set; }

        public double Sum { get; set; }
        public int Count { get; set; }

        public double Mean => Sum / Count;

        public ulong Key => ((ulong)R) << 32 | (uint)C;

        public static int RowFromKey(ulong key)
        {
            return (int)(key >> 32);
        }

        public static int ColumnFromKey(ulong key)
        {
            return (int)(key & 0xFFFFFFFF);
        }

        public AValue(int rank, int row, int column, double sum, int count)
        {
            Rank = rank;
            R = row;
            C = column;
            Sum = sum;
            Count = count;
        }
    }
}
