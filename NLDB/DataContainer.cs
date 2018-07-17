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
        private Alphabet alphabet = new Alphabet();
        private Dictionary<int, Word> i2w = new Dictionary<int, Word>();
        private Dictionary<Word, int> w2i = new Dictionary<Word, int>();
        private Dictionary<Sequence, Link[]> links = new Dictionary<Sequence, Link[]>();

    }

    class DBHelper
    {
        int records_pack_size = 1 << 16;

        public void CreateTable(string dbname, string tablename, string[] columns, bool dropifexists = true)
        {
            StringBuilder command_text = new StringBuilder();
            if (dropifexists) command_text.Append($"DROP TABLE IF EXISTS {tablename};");
            string columns_text = columns.Aggregate("", (c, n) => c == "" ? n : c + ", " + n);
            command_text.Append($"CREATE TABLE {tablename}({columns_text});");
            ExecuteNonQuery(dbname, command_text.ToString());
        }

        public void InsertValues(string dbname, string tablename, string[] columns, IEnumerable<string[]> values)
        {
            StringBuilder command_text = new StringBuilder();
            string columns_text = columns.Aggregate("", (c, n) => c == "" ? n : c + ", " + n);
            string values_text =
                values.Aggregate("", (c, n) =>
                {
                    string t = "(" + n.Aggregate("", (cur, next) => cur == "" ? next : cur + "," + next) + ")";
                    if (c == "")
                        return t;
                    else
                        return c + "," + t;
                });
            command_text.Append($"INSERT INTO {tablename}({columns_text}) VALUES({values_text})");
            command_text.Append(values_text);
            ExecuteNonQuery(dbname, command_text.ToString());
        }

        private void ExecuteNonQuery(string dbfilename, string text)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(dbfilename))
                throw new ArgumentException("Имя базы данных и текст запроса не может быть пустым!");
            using (var db = new SQLiteConnection($"Data Source={dbfilename}; Version=3;"))
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
                    throw new FileNotFoundException($"Ошибка выполнения запроса {text} БД({dbfilename}): {e.Message}");
                }
                finally
                {
                    db.Close();
                }
            }
        }

    }
}
