using SQLite;

namespace NLDB.DAL
{
    [Table("MeanDistancesTable")]
    public class MeanDistance
    {
        [Indexed, NotNull]
        public int Rank { get; set; }

        [Indexed, NotNull]
        public int R { get; set; }

        [Indexed, NotNull]
        public int C { get; set; }

        public int Sum { get; set; }
        public int Count { get; set; }

        [Ignore]
        public float Mean { get => (float)Sum / Count; }
    }
}
