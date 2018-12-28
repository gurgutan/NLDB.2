using SQLite;

namespace NLDB.DAL
{
    [Table("ParentsTable")]
    public class Parent
    {
        [Indexed, NotNull]
        public int WordId { get; set; }

        [Indexed, NotNull]
        public int ParentId { get; set; }
    }
}
