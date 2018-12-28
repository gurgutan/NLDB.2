using SQLite;

namespace NLDB.DAL
{
    [Table("ParentsTable")]
    public class Parent
    {
        public Parent(int wordId, int parentId)
        {
            WordId = wordId;
            ParentId = parentId;
        }

        [Indexed, NotNull]
        public int WordId { get; set; }

        [Indexed, NotNull]
        public int ParentId { get; set; }


    }
}
