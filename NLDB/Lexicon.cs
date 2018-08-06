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
        private SQLiteConnection db;
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
            db = new SQLiteConnection($"Data Source={dbname}; Version=3;");
            db.Open();
        }

        public void Create()
        {
            string words_table =
                $"DROP TABLE words;" +
                $"CREATE TABLE words(id INTEGER NOT NULL, rank INTEGER NOT NULL, parent INTEGER, pos INTEGER, symbol TEXT)," +
                $"PRIMARY KEY (id, parent, pos);";
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

        public Term Get(int id)
        {
            var cmd = db.CreateCommand();
            cmd.CommandText =
                $"SELECT words.rank, words.symbol " +
                $"FROM words " +
                $"WHERE words.id={id} "+
                $"LIMIT 1";
            var reader = cmd.ExecuteReader();
            if(!reader.Read()) return null;
            int rank = reader.GetInt32(0);
            string symbol = reader.GetString(1);
            return new Term(rank, id, 1, symbol, null, null);
        }

        public List<Term> GetChilds(int id)
        {
            var cmd = db.CreateCommand();
            cmd.CommandText =
                $"SELECT words.id, words.rank, words.symbol, words.pos " +
                $"FROM words " +
                $"WHERE words.parent={id} "+
                $"ORDER BY words.pos";
            var reader = cmd.ExecuteReader();
            return CreateTermsFromReader(reader);
        }

        public List<Term> GetChilds(int[] ids)
        {
            string str_ids = ids.Aggregate("", (c, n) => c + (c == "" ? "" : ",") + n.ToString());
            var cmd = db.CreateCommand();
            cmd.CommandText =
                $"SELECT words.id, words.rank, words.symbol, words.pos " +
                $"FROM words " +
                $"words.parent IN ({str_ids}) " +
                $"ORDER BY words.id, words.pos";
            var reader = cmd.ExecuteReader();
            return CreateTermsFromReader(reader);
        }

        public SQLiteDataReader GetSiblingsByParents(int[] ids)
        {
            string str_ids = ids.Aggregate("", (c, n) => c + (c == "" ? "" : ",") + n.ToString());
            var cmd = db.CreateCommand();
            cmd.CommandText =
                $"SELECT DISTINCT sibling.id, sibling.parent" +
                $"FROM words inner join words sibling on sibling.parent = words.parent" +
                $"WHERE words.id IN ({str_ids})";
            throw new NotImplementedException();
        }

        private List<Term> CreateTermsFromReader(SQLiteDataReader reader)
        {
            List<Term> result = new List<Term>();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);     
                int rank = reader.GetInt32(1);
                string symbol = reader.GetString(2);
                result.Add(new Term(rank, id, 1, symbol));
            }
            return result;
        }

    }
}
