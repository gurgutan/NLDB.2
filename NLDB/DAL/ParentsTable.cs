using SQLite;

namespace NLDB.DAL
{
    [Table("ParentsTable")]
    public class ParentsTable
    {
        [Indexed, NotNull]
        public int WordId { get; set; }

        [Indexed, NotNull]
        public int ParentId { get; set; }
    }
}
