namespace NLDB.DAL
{
    public class AValue
    {
        public int Rank { get; set; }

        public int R { get; set; }

        public int C { get; set; }

        public double Sum { get; set; }
        public int Count { get; set; }

        public double Mean { get => Sum / Count; }

        public AValue()
        {
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
