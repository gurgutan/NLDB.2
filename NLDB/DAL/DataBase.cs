using SQLite;


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
            return Table<Word>().Where(w => w.Childs == childs).FirstOrDefault();
        }

        public int Add(Word w)
        {
            return Insert(w);
        }

        internal Word GetWordBySymbol(string s)
        {
            return Table<Word>().Where(w => w.Symbol == s).FirstOrDefault();
        }
    }
}
