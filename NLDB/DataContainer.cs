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

    public static class SQLiteHelper
    {
        private static int records_pack_size = 1 << 16;

        public static void CreateTable(string dbname, string tablename, string[] columns, bool dropifexists = true)
        {
            StringBuilder command_text = new StringBuilder();
            if (dropifexists) command_text.Append($"DROP TABLE IF EXISTS {tablename};");
            string columns_text = columns.Aggregate("", (c, n) => c == "" ? n : c + ", " + n);
            command_text.Append($"CREATE TABLE {tablename}({columns_text});");
            ExecuteNonQuery(dbname, command_text.ToString());
        }

        public static void InsertValues(string dbname, string tablename, string[] columns, IEnumerable<string[]> values)
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

        public static List<string[]> SelectValues(string dbname, string tablename, string[] columns = null, string where = "", string order = "")
        {
            StringBuilder command_text = new StringBuilder();
            string columns_text = "*";
            if (columns != null)
                columns_text = columns.Aggregate("", (c, n) => c == "" ? n : c + ", " + n);
            string where_text = $" WHERE {where}";
            string order_text = $" ORDER BY {order}";
            if (!string.IsNullOrEmpty(columns_text)) command_text.Append($"SELECT {columns_text} FROM {tablename}");
            if (!string.IsNullOrEmpty(where)) command_text.Append(where_text);
            if (!string.IsNullOrEmpty(order_text)) command_text.Append(order_text);
            List<string[]> values = new List<string[]>();
            var db = OpenConnection(dbname);
            var reader = CreateReader(db, command_text.ToString());
            while (reader.Read())
            {
                string[] row = new string[columns.Length];
                reader.GetValues(row);
                values.Add(row);
            }
            return values;
        }

        private static void ExecuteNonQuery(string dbfilename, string text)
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

        private static SQLiteDataReader CreateReader(string dbfilename, string text)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(dbfilename))
                throw new ArgumentException("Имя базы данных и текст запроса не может быть пустым!");
            var db = new SQLiteConnection($"Data Source={dbfilename}; Version=3;");
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
                throw new FileNotFoundException($"Ошибка выполнения запроса {{\n{text}\n}} \nБД({dbfilename}): {e.Message}");
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

        private static SQLiteConnection OpenConnection(string dbfilename)
        {
            var db = new SQLiteConnection($"Data Source={dbfilename}; Version=3;");
            try
            {
                db.Open();
                return db;
            }
            catch (Exception e)
            {
                throw new Exception($"Ошибка открытия соединения с БД({dbfilename}): {e.Message}");
            }
        }
    }
}
