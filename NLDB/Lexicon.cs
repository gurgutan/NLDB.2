using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;

namespace NLDB
{
    public class Lexicon
    {
        private SQLiteTransaction transaction;
        private SQLiteConnection db;
        private int current_id = 0;
        private string dbname;
        private string[] splitters;
        public string[] Splitters
        {
            get { return splitters; }
            set { splitters = value; }
        }

        public Lexicon(string _dbname, string[] _splitters)
        {
            dbname = _dbname;
            splitters = _splitters;
            var db = new SQLiteConnection($"Data Source={dbname}; Version=3;");
            db.Open();
        }

        public void Create()
        {
            string words_table = 
                $"DROP TABLE words;"+
                $"CREATE TABLE words(id INTEGER PRIMARY KEY, rank INTEGER NOT NULL, parent INTEGER, pos INTEGER, symbol TEXT);";
            string links_table =
                $"DROP TABLE links;" +
                $"CREATE TABLE links(seq TEXT PRIMARY KEY, next INTEGER NOT NULL, count INTEGER NOT NULL);";
            string splitters_table =
                $"DROP TABLE splitters;" +
                $"CREATE TABLE splitters(rank INTEGER PRIMARY KEY, text TEXT);";
            var db = new SQLiteConnection($"Data Source={dbname}; Version=3;");
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = words_table + links_table;
            cmd.ExecuteNonQuery();
        }

        //public SQLiteData Query(string query)
        //{
        //    var cmd = db.CreateCommand();
        //    cmd.CommandText = query;
        //    var result = cmd.ExecuteReader();
        //    result.DataTable.
        //}

        public SQLiteDataReader GetChilds(int id)
        {
            var cmd = db.CreateCommand();
            cmd.CommandText =
                "SELECT words.id, words.rank, words.symbol, words.pos " +
                "FROM words "+
                $"WHERE words.parent={id}";
            throw new NotImplementedException();
        }

        public SQLiteDataReader GetChilds(int[] ids)
        {
            string str_ids = ids.Aggregate("", (c, n) => c + (c == "" ? "" : ",") + n.ToString());
            var cmd = db.CreateCommand();
            cmd.CommandText =
                "SELECT words.id, words.rank, words.symbol " +
                "FROM words " +
                $"WHERE words.parent IN ({str_ids})";
            throw new NotImplementedException();
        }

        public SQLiteDataReader GetSiblings(int[] ids)
        {
            string str_ids = ids.Aggregate("", (c, n) => c + (c == "" ? "" : ",") + n.ToString());
            var cmd = db.CreateCommand();
            cmd.CommandText =
                "SELECT DISTINCT words2.id AS 'child', words2.parent AS 'parent'" +
                "FROM words words1 inner join words words2 on words2.parent = words1.parent" +
                $"WHERE words1.id IN ({str_ids})";
            throw new NotImplementedException();
        }

    }
}
