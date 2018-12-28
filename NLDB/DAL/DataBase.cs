using SQLite;
using System.Linq;

namespace NLDB.DAL
{
    public class DataBase : SQLiteConnection
    {
        public DataBase(string databasePath) : base(databasePath, true)
        {
            Trace = false;
        }

        public void Create()
        {
            CreateTable<Splitter>();
            CreateTable<Word>();
            CreateTable<Parent>();
            CreateTable<MeanDistance>();
            CreateTable<WordsSimilarity>();
        }

        internal Word GetWordByChilds(string childs)
        {
            return Query<Word>("select * from WordsTable where Childs = ?", childs).FirstOrDefault();
            //return Table<Word>().Where(w => w.Childs == childs).FirstOrDefault();            
        }

        public int Add(Word w)
        {
            int id = Insert(w);
            return id;
            //Insert<Parent>(new Parent(id,))

        }

        internal Word GetWordBySymbol(string s)
        {
            return Table<Word>().Where(w => w.Symbol == s).FirstOrDefault();
        }
    }
}
