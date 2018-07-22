using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public static class SQLiteHelper
    {
        private static UInt64 records_block_size = 1 << 12;

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
            string columns_text = columns.Aggregate("", (c, n) => c == "" ? n : c + ", " + n);
            StringBuilder val_text = new StringBuilder();
            UInt64 records_count = 0;
            foreach (var v in values)
            {
                string t = "(" + v.Aggregate("", (cur, next) => cur == "" ? @"""" + next + @"""" : cur + "," + @"""" + next + @"""") + ")";
                if (val_text.Length > 0) val_text.Append(",");
                val_text.Append(t);
                records_count++;
                if (records_block_size % records_count == 0)
                {
                    ExecuteNonQuery(dbname, $"INSERT INTO {tablename}({columns_text}) VALUES {val_text}");
                    val_text.Clear();
                    GC.Collect();
                }
            }
            //Если в последней итерации цикла значения не были записаны в БД, то сдлелаем это сейчас
            if (records_block_size % records_count != 0)
                ExecuteNonQuery(dbname, $"INSERT INTO {tablename}({columns_text}) VALUES {val_text}");
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

        /// <summary>
        /// Запрашивает значения из БД по открытому ранее соединению db
        /// </summary>
        /// <param name="db"></param>
        /// <param name="tablename"></param>
        /// <param name="columns">перечисление столбцов через запятую</param>
        /// <param name="where"></param>
        /// <param name="limit">смещение (относительно начала) и количество строк через запятую в результате: "offset, row_count"</param>
        /// <param name="order"></param>
        /// <returns></returns>
        public static List<string[]> SelectValues(SQLiteConnection db, string tablename, string columns = "", string where = "", string limit = "", string order = "")
        {
            string cmd_text = $"SELECT {columns} FROM {tablename}" +
                 (where == "" ? "" : " WHERE " + where) +
                 (limit == "" ? "" : " LIMIT " + limit) +
                 (order == "" ? "" : " ORDER BY " + order);
            List<string[]> values = new List<string[]>();
            SQLiteDataReader reader;
            try { reader = ExecuteQuery(db, cmd_text); }
            catch (SQLiteException e) { throw new SQLiteException($"Ошибка выполнения запроса {cmd_text} БД({db.FileName}): {e.Message}"); }

            while (reader.Read())
            {
                string[] row = new string[columns.Length];
                reader.GetValues(row);
                values.Add(row);
            }
            return values;
        }

        public static object SelectScalar(SQLiteConnection db, string tablename, string columns = "", string where = "")
        {
            string cmd_text = $"SELECT {columns} FROM {tablename}" + (where == "" ? "" : " WHERE " + where) + " LIMIT 1;";
            return ExecuteScalar(db, cmd_text);
        }

        public static void CreateIndex(string dbname, string tablename, string indexname, string[] columns, bool unique = false)
        {
            StringBuilder cmd_text = new StringBuilder("CREATE");
            if (unique) cmd_text.Append(" UNIQUE ");
            string col_text = columns.Aggregate("", (c, n) => c == "" ? n : c + "," + n);
            cmd_text.Append($" INDEX {indexname} ON {tablename}({col_text})");
            ExecuteNonQuery(dbname, cmd_text.ToString());
        }


        //---------------------------------------------------------------------------------------------------------------------------
        private static object ExecuteScalar(SQLiteConnection db, string text)
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = text;
            try { return cmd.ExecuteScalar(); }
            catch (SQLiteException e) { throw new SQLiteException($"Ошибка выполнения запроса {text} БД({db.FileName}): {e.Message}"); }
        }

        private static SQLiteDataReader ExecuteQuery(SQLiteConnection db, string text)
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = text;
            try { return cmd.ExecuteReader(); }
            catch (SQLiteException e) { throw new SQLiteException($"Ошибка выполнения запроса {text} БД({db.FileName}): {e.Message}"); }
        }

        private static SQLiteDataReader ExecuteQuery(string dbname, string text)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(dbname))
                throw new ArgumentException("Имя базы данных и текст запроса не может быть пустым!");
            using (var db = new SQLiteConnection($"Data Source={dbname}; Version=3;"))
            {
                db.Open();
                SQLiteCommand cmd = db.CreateCommand();
                cmd.CommandText = text;
                try { return cmd.ExecuteReader(); }
                catch (SQLiteException e) { throw new SQLiteException($"Ошибка выполнения запроса {text} БД({dbname}): {e.Message}"); }
                finally { db.Close(); }
            }
        }

        private static void ExecuteNonQuery(string dbname, string text)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(dbname))
                throw new ArgumentException("Имя базы данных и текст запроса не может быть пустым!");
            using (var db = new SQLiteConnection($"Data Source={dbname}; Version=3;"))
            {
                db.Open();
                SQLiteCommand cmd = db.CreateCommand();
                cmd.CommandText = text;
                try { cmd.ExecuteNonQuery(); }
                catch (SQLiteException e) { throw new SQLiteException($"Ошибка выполнения запроса {text} БД({dbname}): {e.Message}"); }
                finally { db.Close(); }
            }
        }

        private static SQLiteDataReader CreateReader(string dbname, string text)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(dbname))
                throw new ArgumentException("Имя базы данных и текст запроса не может быть пустым!");
            var db = new SQLiteConnection($"Data Source={dbname}; Version=3;");
            db.Open();
            SQLiteCommand cmd = db.CreateCommand();
            try { return cmd.ExecuteReader(); }
            catch (SQLiteException e) { throw new SQLiteException($"Ошибка выполнения запроса {{\n{text}\n}} \nБД({dbname}): {e.Message}"); }
            finally { db.Close(); }
        }

        private static SQLiteDataReader CreateReader(SQLiteConnection db, string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Текст запроса не может быть пустым!");
            if (db.State != System.Data.ConnectionState.Open)
                throw new ArgumentException("Соединение с БД должно быть открытым!");
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = text;
            try { return cmd.ExecuteReader(); }
            catch (SQLiteException e) { throw new SQLiteException($"Ошибка выполнения запроса {{\n{text}\n}} \nБД({db.FileName}): {e.Message}"); }
        }

        public static SQLiteConnection OpenConnection(string dbname)
        {
            var db = new SQLiteConnection($"Data Source={dbname}; Version=3;");
            try
            {
                db.Open();
                return db;
            }
            catch (SQLiteException e)
            {
                throw new SQLiteException($"Ошибка открытия соединения с БД({dbname}): {e.Message}");
            }
        }

        public static void CloseConnection(SQLiteConnection connection)
        {
            if (connection.State == System.Data.ConnectionState.Open)
                connection.Close();
            else
                throw new ArgumentException($"Соединение БД {connection.FileName} не открыто!");
        }
    }
}
