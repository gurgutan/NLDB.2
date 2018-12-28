using System;
using System.Linq;
using SQLite;

namespace NLDB.DAL
{
    public class DataBase : SQLiteConnection
    {
        int Id = 0;
        public DataBase(string databasePath) : base(databasePath, true)
        {
            Trace = false;
        }

        public void Create()
        {
            DropTable<Splitter>();
            DropTable<Word>();
            DropTable<Parent>();
            DropTable<MeanDistance>();
            DropTable<WordsSimilarity>();
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
            int id = NextId();
            return Execute($"INSERT INTO WordsTable(Id, Rank, Symbol, Childs) VALUES (?,?,?,?);", Id, w.Rank, w.Symbol, w.Childs);
            //Insert<Parent>(new Parent(id,))

        }

        private int NextId()
        {
            return ++Id;
        }

        internal Word GetWordBySymbol(string s)
        {
            return Table<Word>().Where(w => w.Symbol == s).FirstOrDefault();
        }
    }
}
