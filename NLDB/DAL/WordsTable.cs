using SQLite;

namespace NLDB.DAL
{
    [Table("WordsTable")]
    public class WordsTable
    {
        [PrimaryKey, NotNull]
        public int Id { get; set; }

        [Indexed, NotNull]
        public int Rank { get; set; }

        [Indexed]
        public string Symbol { get; set; }

        [Indexed]
        public string Childs { get; set; }
    }
}
