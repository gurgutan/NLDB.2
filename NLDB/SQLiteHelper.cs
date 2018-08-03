using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    using Values = IEnumerable<string[]>;

    public static class SQLiteHelper
    {
        private static UInt64 records_block_size = 1 << 19;

        public static void CreateTable(string dbname, string tablename, string columns, bool dropifexists = true)
        {
            StringBuilder cmd_text = new StringBuilder();
            if (dropifexists) cmd_text.Append($"DROP TABLE IF EXISTS {tablename};");
            cmd_text.Append($"CREATE TABLE {tablename}({columns});");
            ExecuteNonQuery(dbname, cmd_text.ToString());
        }

        public static void InsertValues(string dbname, string tablename, string columns, Values values)
        {
            SQLiteConnection db = OpenConnection(dbname);
            SQLiteHelper.InsertValues(db, tablename, columns, values);
            db.Close();
        }

        public static void InsertValues(SQLiteConnection db, string tablename, string columns, Values values)
        {
            if (values.Count() == 0) return;
            StringBuilder val_text = new StringBuilder();
            UInt64 records_count = 0;
            foreach (var v in values)
            {
                if (val_text.Length > 0) val_text.Append(',');
                val_text.Append(ToValueText(v));
                if (records_block_size % (++records_count) == 0)
                {
                    ExecuteNonQuery(db, $"INSERT INTO {tablename}({columns}) VALUES {val_text.ToString()}");
                    val_text.Clear();
                }
            }
            //Если в последней итерации цикла значения не были записаны в БД, то сдлелаем это сейчас
            if (records_block_size % records_count != 0)
                ExecuteNonQuery(db, $"INSERT INTO {tablename}({columns}) VALUES {val_text}");
        }

        public static List<string[]> SelectValues(string dbname, string tablename, string columns = "*", string where = "", string order = "")
        {
            SQLiteConnection db = OpenConnection(dbname);
            var result = SQLiteHelper.SelectValues(db, tablename, columns, where, order);
            db.Close();
            return result;
        }

        public static List<string[]> SelectValues(SQLiteConnection db, string tablename, string columns = "*", string where = "", string limit = "", string order = "")
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

        public static void CreateIndex(string dbname, string tablename, string indexname, string columns, bool unique = false)
        {
            StringBuilder cmd_text = new StringBuilder("CREATE");
            if (unique) cmd_text.Append(" UNIQUE ");
            cmd_text.Append($" INDEX {indexname} ON {tablename}({columns})");
            ExecuteNonQuery(dbname, cmd_text.ToString());
        }

        public static void ReplaceValues(string dbname, string tablename, string columns, Values data)
        {
            SQLiteConnection db = SQLiteHelper.OpenConnection(dbname);
            SQLiteHelper.ReplaceValues(db, tablename, columns, data);
            db.Close();
        }

        public static void ReplaceValues(SQLiteConnection db, string tablename, string columns, Values data)
        {
            string val_text = data.Aggregate("", (c, n) => c + (c == "" ? "" : ",") + ToValueText(n));
            string cmd_text = $"REPLACE INTO {tablename}({columns}) VALUES {val_text};";
            ExecuteScalar(db, cmd_text.ToString());
        }

        public static void DropTable(SQLiteConnection db, string tablename)
        {
            string cmd_text = $"DROP TABLE {tablename};";
            ExecuteScalar(db, cmd_text);
        }

        public static void DropTable(string dbname, string tablename)
        {
            string cmd_text = $"DROP TABLE {tablename};";
            ExecuteNonQuery(dbname, cmd_text.ToString());
        }

        public static int Count(SQLiteConnection db, string tablename) =>
            int.Parse(ExecuteScalar(db, $"SELECT COUNT(*) FROM {tablename};").ToString());

        public static int Max(SQLiteConnection db, string tablename, string column) =>
            int.Parse(ExecuteScalar(db, $"SELECT MAX(cast({column} as INTEGER)) FROM {tablename};").ToString());

        //---------------------------------------------------------------------------------------------------------------------------
        internal static string ToValueText(string[] value) =>
            "(" + value.Aggregate("", (c, n) => c + (c == "" ? "" : ", ") + "'" + n + "'") + ")";

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

        private static void ExecuteNonQuery(SQLiteConnection db, string text)
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = text;
            try { cmd.ExecuteScalar(); }
            catch (SQLiteException e) { throw new SQLiteException($"Ошибка выполнения запроса {text} БД({db.FileName}): {e.Message}"); }
        }

        internal static SQLiteDataReader CreateReader(string dbname, string text)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(dbname))
                throw new ArgumentException("Имя базы данных и текст запроса не может быть пустым!");
            var db = new SQLiteConnection($"Data Source={dbname}; Version=3;");
            db.Open();
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = text;
            try { return cmd.ExecuteReader(); }
            catch (SQLiteException e) { throw new SQLiteException($"Ошибка выполнения запроса {{\n{text}\n}} \nБД({dbname}): {e.Message}"); }
            finally { db.Close(); }
        }

        internal static SQLiteDataReader CreateReader(SQLiteConnection db, string text)
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
            //else
            //    throw new ArgumentException($"Соединение БД {connection.FileName} не открыто!");
        }

        private static Values GetRange(Values values, int from, int count) =>
            values.Skip(from == 0 ? 0 : from - 1).Take(count);

    }
}
