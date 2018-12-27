using SQLite;

namespace NLDB.DAL
{
    [Table("WordsSimilarityTable")]
    public class WordsSimilarityTable
    {
        [Indexed, NotNull]
        public int Rank { get; set; }

        [Indexed, NotNull]
        public int R { get; set; }

        [Indexed, NotNull]
        public int C { get; set; }

        public int Similarity { get; set; }
    }
}
