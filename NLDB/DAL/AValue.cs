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
    }
}
