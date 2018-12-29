
namespace NLDB.DAL
{
    public class Parent
    {
        public Parent(int wordId, int parentId)
        {
            WordId = wordId;
            ParentId = parentId;
        }

        public int WordId { get; set; }

        public int ParentId { get; set; }
    }
}
