namespace NLDB.DAL
{
    public class AValue
    {
        public int Rank { get; set; }

        public int R { get; set; }

        public int C { get; set; }

        public int Sum { get; set; }
        public int Count { get; set; }

        public float Mean { get => (float)Sum / Count; }

        public AValue()
        {
        }

        public AValue(int rank, int r, int c, int sum, int count)
        {
            Rank = rank;
            R = r;
            C = c;
            Sum = sum;
            Count = count;
        }
    }
}
