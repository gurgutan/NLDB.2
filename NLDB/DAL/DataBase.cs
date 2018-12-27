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
            using (var db = new SQLiteConnection(DatabasePath))
            {
                db.CreateTable<SplittersTable>();
                db.CreateTable<WordsTable>();
                db.CreateTable<ParentsTable>();
                db.CreateTable<MeanDistancesTable>();
                db.CreateTable<WordsSimilarityTable>();
            }
        }

    }
}
