using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public class DataContainer
    {
        private string[] splitters;
        private Alphabet alphabet = new Alphabet();
        private Dictionary<int, Word> i2w = new Dictionary<int, Word>();
        private Dictionary<Word, int> w2i = new Dictionary<Word, int>();
        private Dictionary<Sequence, Link[]> links = new Dictionary<Sequence, Link[]>();

        private string dbname = "lang.db";

        public DataContainer(string _dbname)
        {
            dbname = _dbname;
        }

        public DataContainer(
            string[] _splitters,
            Alphabet _alphabet,
            Dictionary<int, Word> _i2w,
            Dictionary<Word, int> _w2i,
            Dictionary<Sequence, Link[]> _links)
        {
            splitters = _splitters;
            alphabet = _alphabet;
            i2w = _i2w;
            w2i = _w2i;
            links = _links;
        }

        public void Save(string _dbname)
        {
            dbname = _dbname;
            SaveSplitters();
            SaveAlphabet();
            SaveWords();
            SaveLinks();
        }

        private void SaveSplitters()
        {
            string[] columns = new string[] { "rank", "expr" };
            var data = splitters.Select((s, i) => new string[2] { s, i.ToString() });
            SQLiteHelper.CreateTable(dbname, "alphabet", columns, true);
            SQLiteHelper.InsertValues(dbname, "alphabet", columns, data);
        }

        private void SaveAlphabet()
        {
            string[] columns = new string[] { "code", "letter" };
            var data = alphabet.Letters.Select(kvp => new string[2] { kvp.Key.ToString(), kvp.Value });
            SQLiteHelper.CreateTable(dbname, "alphabet", columns, true);
            SQLiteHelper.InsertValues(dbname, "alphabet", columns, data);
            SQLiteHelper.CreateIndex(dbname, "alphabet", "code_ind", new string[] { "code" });
            SQLiteHelper.CreateIndex(dbname, "alphabet", "letter_ind", new string[] { "letter" });
        }

        private void SaveWords()
        {
            string[] columns_words = new string[] { "id", "rank" };
            string[] columns_childs = new string[] { "id", "child_id" };
            string[] columns_parents = new string[] { "id", "parent_id" };
            var words_data = i2w.Values.Select(w => new string[2] { w.id.ToString(), w.rank.ToString() }).Distinct();
            var childs_data = i2w.Values.SelectMany(w => w.childs.Select(c => new string[2] { w.id.ToString(), c.ToString() }));
            var parents_data = i2w.Values.SelectMany(w => w.parents.Select(p => new string[2] { w.id.ToString(), p.ToString() }));
            //Создаем таблицы
            SQLiteHelper.CreateTable(dbname, "words", columns_words, true);
            SQLiteHelper.CreateTable(dbname, "childs", columns_childs, true);
            SQLiteHelper.CreateTable(dbname, "parents", columns_parents, true);
            //Добавляем данные
            SQLiteHelper.InsertValues(dbname, "words", columns_words, words_data);
            SQLiteHelper.InsertValues(dbname, "childs", columns_childs, childs_data);
            SQLiteHelper.InsertValues(dbname, "parents", columns_parents, parents_data);
            //Создаем индексы
            SQLiteHelper.CreateIndex(dbname, "words", "words_id_ind", new string[] { "id" });
            SQLiteHelper.CreateIndex(dbname, "childs", "childs_id_ind", new string[] { "id" });
            SQLiteHelper.CreateIndex(dbname, "parents", "parents_id_ind", new string[] { "id" });
        }

        private void SaveLinks()
        {
            string[] columns_links = new string[] { "seq", "id", "confidence" };
            var links_data = links.SelectMany(kvp => kvp.Value.
                Select(l => new string[3]
                    {
                        kvp.Key.sequence.Aggregate("", (c, n) => c == "" ? n.ToString() : c + "," + n.ToString()),  //seq
                        l.id.ToString(),            //id
                        l.confidence.ToString()     //confidence
                    }));
            SQLiteHelper.CreateTable(dbname, "links", columns_links, true);
            SQLiteHelper.InsertValues(dbname, "links", columns_links, links_data);
            SQLiteHelper.CreateIndex(dbname, "links", "seq_ind", new string[] { "seq" });

        }

        public void Load(string _dbname)
        {
            dbname = _dbname;
            throw new NotImplementedException();
        }

    }



    public static class SQLiteHelper
    {
        private static int records_pack_size = 1 << 16;

        public static void CreateTable(string dbname, string tablename, string[] columns, bool dropifexists = true)
        {
            StringBuilder cmd_text = new StringBuilder();
            if (dropifexists) cmd_text.Append($"DROP TABLE IF EXISTS {tablename};");
            string columns_text = columns.Aggregate("", (c, n) => c == "" ? n : c + " TEXT NOT NULL, " + n);
            cmd_text.Append($"CREATE TABLE {tablename}({columns_text});");
            ExecuteNonQuery(dbname, cmd_text.ToString());
        }

        public static void InsertValues(string dbname, string tablename, string[] columns, IEnumerable<string[]> values)
        {
            StringBuilder cmd_text = new StringBuilder();
            string columns_text = columns.Aggregate("", (c, n) => c == "" ? n : c + ", " + n);
            StringBuilder val_text = new StringBuilder();
            foreach (var v in values)
            {
                string t = "(" + 
                    v.Aggregate("", (cur, next) => cur == "" ? @"""" + next + @"""" : cur + "," + @"""" + next + @"""") + 
                    ")";
                if (val_text.Length > 0) val_text.Append(",");
                val_text.Append(t);
            }
            cmd_text.Append($"INSERT INTO {tablename}({columns_text}) VALUES {val_text}");
            ExecuteNonQuery(dbname, cmd_text.ToString());
        }

        public static List<string[]> SelectValues(string dbname, string tablename, string[] columns = null, string where = "", string order = "")
        {
            StringBuilder cmd_text = new StringBuilder();
            string columns_text = "*";
            if (columns != null)
                columns_text = columns.Aggregate("", (c, n) => c == "" ? n : c + ", " + n);
            string where_text = $" WHERE {where}";
            string order_text = $" ORDER BY {order}";
            if (!string.IsNullOrEmpty(columns_text)) cmd_text.Append($"SELECT {columns_text} FROM {tablename}");
            if (!string.IsNullOrEmpty(where)) cmd_text.Append(where_text);
            if (!string.IsNullOrEmpty(order_text)) cmd_text.Append(order_text);
            List<string[]> values = new List<string[]>();
            var db = OpenConnection(dbname);
            var reader = CreateReader(db, cmd_text.ToString());
            while (reader.Read())
            {
                string[] row = new string[columns.Length];
                reader.GetValues(row);
                values.Add(row);
            }
            return values;
        }

        public static void CreateIndex(string dbname, string tablename, string indexname, string[] columns, bool unique = false)
        {
            StringBuilder cmd_text = new StringBuilder("CREATE");
            if (unique) cmd_text.Append(" UNIQUE ");
            string col_text = columns.Aggregate("", (c, n) => c == "" ? n : c + "," + n);
            cmd_text.Append($" INDEX {indexname} ON {tablename}({col_text})");
            ExecuteNonQuery(dbname, cmd_text.ToString());
        }

        private static void ExecuteNonQuery(string dbname, string text)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(dbname))
                throw new ArgumentException("Имя базы данных и текст запроса не может быть пустым!");
            using (var db = new SQLiteConnection($"Data Source={dbname}; Version=3;"))
            {
                db.Open();
                SQLiteCommand cmd = db.CreateCommand();
                try
                {
                    cmd.CommandText = text;
                    cmd.ExecuteNonQuery();
                }
                catch (SQLiteException e)
                {
                    throw new FileNotFoundException($"Ошибка выполнения запроса {text} БД({dbname}): {e.Message}");
                }
                finally
                {
                    db.Close();
                }
            }
        }

        private static SQLiteDataReader CreateReader(string dbname, string text)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(dbname))
                throw new ArgumentException("Имя базы данных и текст запроса не может быть пустым!");
            var db = new SQLiteConnection($"Data Source={dbname}; Version=3;");
            db.Open();
            SQLiteCommand cmd = db.CreateCommand();
            try
            {
                cmd.CommandText = text;
                var reader = cmd.ExecuteReader();
                return reader;
            }
            catch (SQLiteException e)
            {
                db.Close();
                throw new FileNotFoundException($"Ошибка выполнения запроса {{\n{text}\n}} \nБД({dbname}): {e.Message}");
            }
        }

        private static SQLiteDataReader CreateReader(SQLiteConnection db, string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Текст запроса не может быть пустым!");
            if (db.State != System.Data.ConnectionState.Open)
                throw new ArgumentException("Соединение с БД должно быть открытым!");
            SQLiteCommand cmd = db.CreateCommand();
            try
            {
                cmd.CommandText = text;
                var reader = cmd.ExecuteReader();
                return reader;
            }
            catch (SQLiteException e)
            {
                throw new FileNotFoundException($"Ошибка выполнения запроса {{\n{text}\n}} \nБД({db.FileName}): {e.Message}");
            }
        }

        private static SQLiteConnection OpenConnection(string dbname)
        {
            var db = new SQLiteConnection($"Data Source={dbname}; Version=3;");
            try
            {
                db.Open();
                return db;
            }
            catch (Exception e)
            {
                throw new Exception($"Ошибка открытия соединения с БД({dbname}): {e.Message}");
            }
        }
    }
}
