namespace NLDB.DAL
{
    public class BValue
    {
        public int Rank { get; set; }

        public int R { get; set; }

        public int C { get; set; }

        public double Similarity { get; set; }

        public BValue(int rank, int row, int column, double s)
        {
            Rank = rank;
            R = row;
            C = column;
            Similarity = s;
        }
    }
}
