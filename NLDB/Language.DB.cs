using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;

//Структура БД

namespace NLDB
{
    public partial class Language
    {

        public static void CreateDB(string filename)
        {
            using (var db = new SQLiteConnection("Data Source=" + filename + "; Version=3;"))
            {
                db.Open();
                string create_word =
                    "DROP TABLE IF EXISTS word;" +
                    "CREATE TABLE word(id INTEGER NOT NULL, rank INTEGER NOT NULL);";
                string create_childs =
                    "DROP TABLE IF EXISTS childs;" +
                    "CREATE TABLE childs(id INTEGER NOT NULL, rank INTEGER NOT NULL, child INTEGER NOT NULL)";
                string create_atoms =
                    "DROP TABLE IF EXISTS atoms;" +
                    "CREATE TABLE atoms(id INTEGER PRIMARY KEY, name TEXT NOT NULL)";
                SQLiteCommand cmd_create = db.CreateCommand();
                try
                {
                    cmd_create.CommandText = create_word;
                    cmd_create.ExecuteNonQuery();
                    cmd_create.CommandText = create_childs;
                    cmd_create.ExecuteNonQuery();
                    cmd_create.CommandText = create_atoms;
                    cmd_create.ExecuteNonQuery();
                }
                catch (SQLiteException ex)
                {
                    Console.WriteLine("Ошибка создания базы данных: " + ex.Message);
                }
            }
        }


        public static void Load(string filename)
        {
            using (var db = new SQLiteConnection("Data Source=" + filename + "; Version=3;"))
            {
                db.Open();
            }
        }
    }
}
