using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using NLDB;

//Структура БД

namespace NLDB
{
    //public partial class Language
    //{
    //    public void DBCreate(string filename)
    //    {
    //        using (var db = new SQLiteConnection("Data Source=" + filename + "; Version=3;"))
    //        {
    //            db.Open();
    //            string create_info =
    //                "DROP TABLE IF EXISTS info;" +
    //                "CREATE TABLE info(rank INTEGER NOT NULL, name TEXT NOT NULL, splitters TEXT NOT NULL)";
    //            string create_word =
    //                "DROP TABLE IF EXISTS word;" +
    //                "CREATE TABLE word(rank INTEGER NOT NULL, id INTEGER);";
    //            string create_childs =
    //                "DROP TABLE IF EXISTS childs;" +
    //                "CREATE TABLE childs(rank INTEGER NOT NULL, id INTEGER, child INTEGER NOT NULL, pos INTEGER NOT NULL)";
    //            string create_atoms =
    //                "DROP TABLE IF EXISTS atoms;" +
    //                "CREATE TABLE atoms(rank INTEGER NOT NULL, id INTEGER, name TEXT NOT NULL)";
    //            string create_index_words =
    //                "CREATE INDEX idx_words ON word(id, rank)";
    //            string create_index_childs =
    //                "CREATE INDEX idx_childs ON childs(id, rank, child)";
    //            SQLiteCommand cmd_create = db.CreateCommand();
    //            try
    //            {
    //                cmd_create.CommandText = create_info;
    //                cmd_create.ExecuteNonQuery();
    //                cmd_create.CommandText = create_word;
    //                cmd_create.ExecuteNonQuery();
    //                cmd_create.CommandText = create_childs;
    //                cmd_create.ExecuteNonQuery();
    //                cmd_create.CommandText = create_atoms;
    //                cmd_create.ExecuteNonQuery();
    //                cmd_create.CommandText = create_index_words;
    //                cmd_create.ExecuteNonQuery();
    //                cmd_create.CommandText = create_index_childs;
    //                cmd_create.ExecuteNonQuery();
    //            }
    //            catch (SQLiteException e)
    //            {
    //                throw new FileNotFoundException("Ошибка создания базы данных " + filename + e.Message);
    //            }
    //            finally
    //            {
    //                db.Close();
    //            }
    //        }
    //    }

    //    public void DBSave(string filename)
    //    {
    //        //Попытка создать БД
    //        try
    //        {
    //            DBCreate(filename);
    //        }
    //        catch (FileNotFoundException e)
    //        {
    //            Console.WriteLine(e.Message);
    //            return;
    //        }
    //        //Сохранение данных в БД
    //        using (var db = new SQLiteConnection("Data Source=" + filename + "; Version=3;"))
    //        {
    //            db.Open();
    //            SQLiteCommand cmd_insert = db.CreateCommand();
    //            try
    //            {
    //                string insert_info = InsertInfoCommand();
    //                cmd_insert.CommandText = insert_info;
    //                cmd_insert.ExecuteNonQuery();
    //                for (int rank = 0; rank <= this.Rank; rank++)
    //                {
    //                    Console.WriteLine($"Словарь ранга {rank}");
    //                    //Добавление алфавита
    //                    Console.WriteLine("Добавление алфавита...");
    //                    string insert_atoms = InsertAtomsCommand(rank);
    //                    if (insert_atoms != "")
    //                    {
    //                        cmd_insert.CommandText = insert_atoms;
    //                        cmd_insert.ExecuteNonQuery();
    //                    }
    //                    //Добавление слов
    //                    Console.WriteLine("Добавление слов...");
    //                    string insert_words = InsertWordsCommand(rank);
    //                    if (insert_words != "")
    //                    {
    //                        cmd_insert.CommandText = insert_words;
    //                        cmd_insert.ExecuteNonQuery();
    //                    }
    //                    //Добавление дочерних связей
    //                    Console.WriteLine("Добавление дочерних связей ...");
    //                    string insert_childs = InserChildsCommand(rank);
    //                    if (insert_childs != "")
    //                    {
    //                        cmd_insert.CommandText = insert_childs;
    //                        cmd_insert.ExecuteNonQuery();
    //                    }
    //                }
    //            }
    //            catch (SQLiteException e)
    //            {
    //                Console.WriteLine("Ошибка записи в базу данных: " + e.Message);
    //            }
    //            finally
    //            {
    //                db.Close();
    //            }
    //        }
    //    }

    //    public void DBLoad(string filename)
    //    {
    //        if (!File.Exists(filename))
    //            throw new FileNotFoundException($"Файл {filename} не найден!");
    //        using (var db = new SQLiteConnection("Data Source=" + filename + "; Version=3;"))
    //        {
    //            db.Open();
    //            SQLiteCommand cmd_select = db.CreateCommand();
    //            try
    //            {


    //                LoadWords(db);
    //                LoadAtoms(db);
    //            }
    //            catch (Exception e)
    //            {
    //                Console.WriteLine("Ошибка чтения БД " + filename + ". " + e.Message);
    //            }
    //            finally
    //            {
    //                db.Close();
    //            }
    //        }
    //    }

    //    private void LoadAtoms(SQLiteConnection db)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    private void LoadWords(SQLiteConnection db)
    //    {
    //        SQLiteCommand cmd_select = db.CreateCommand();
    //        string cmd =
    //            @"SELECT [rank], [id], [child], [pos] 
    //            FROM [childs] 
    //            ORDER BY [rank] ASC, [id] ASC, [pos] ASC";
    //        SQLiteDataReader reader = cmd_select.ExecuteReader();
    //        Word word = new Word(-1);
    //        while (reader.Read())
    //        {
    //            int id = Convert.ToInt32(reader["id"]);
    //            int rank = Convert.ToInt32(reader["rank"]);
    //            int child = Convert.ToInt32(reader["child"]);
    //            int pos = Convert.ToInt32(reader["pos"]);
    //            if(word.Id!=id)
    //            {

    //            }
    //        }
    //        reader.Close();

    //        cmd_select.CommandText = "SELECT * FROM info";
    //        cmd_select.ExecuteNonQuery();
    //    }

    //    private void AddWord(Word w)
    //    {

    //    }

    //    private void SelectInfoCommand(SQLiteConnection db)
    //    {
    //        SQLiteCommand cmd_select = db.CreateCommand();
    //        cmd_select.CommandText = "SELECT rank, name, splitters FROM info";
    //        cmd_select.ExecuteNonQuery();
    //    }

    //    private string InsertInfoCommand()
    //    {
    //        return "INSERT INTO info(rank, name, splitters) VALUES(" +
    //            this.Rank + "," +
    //            this.Name + "," +
    //            this.splitters.Aggregate("", (c, n) => c == "" ? n : c + "<:>" + n) +
    //            ")";
    //    }

    //    //Формирование команд добавления в БД (Insert)
    //    private string InserChildsCommand(int rank)
    //    {
    //        StringBuilder childs = new StringBuilder("INSERT INTO childs(id, rank, child, pos) VALUES");
    //        StringBuilder values = new StringBuilder();
    //        foreach (var w in Lexicons[rank].Words)
    //        {
    //            if (w.Childs == null) continue;
    //            for (int i = 0; i < w.Childs.Length; i++)
    //                values.Append("(" + w.Id.ToString() + "," + rank.ToString() + "," + w.Childs[i].ToString() + "," + i.ToString() + "),");
    //        }
    //        //Удаляем последнюю запятую
    //        if (values.Length == 0) return "";
    //        childs.Append(values.Remove(values.Length - 1, 1));
    //        return childs.ToString();
    //    }

    //    private string InsertWordsCommand(int rank)
    //    {
    //        if (Lexicons[rank].Count == 0) return "";
    //        StringBuilder words = new StringBuilder("INSERT INTO word(id, rank) VALUES");
    //        foreach (var w in Lexicons[rank].Words)
    //        {
    //            string word = "(" + w.Id.ToString() + "," + rank.ToString() + "),";
    //            words.Append(word);
    //        }
    //        //Удаляем последнюю запятую
    //        words.Remove(words.Length - 1, 1);
    //        return words.ToString();
    //    }

    //    private string InsertAtomsCommand(int rank)
    //    {
    //        if (Lexicons[rank].Alphabet.Count == 0) return "";
    //        StringBuilder atoms = new StringBuilder("INSERT INTO atoms(id, rank, name) VALUES");
    //        foreach (var a in Lexicons[rank].Alphabet)
    //        {
    //            string atom = "(" + a.Value + "," + rank.ToString() + @",""" + a.Key.ToString() + @"""),";
    //            atoms.Append(atom);
    //        }
    //        //Удаляем последнюю запятую
    //        atoms.Remove(atoms.Length - 1, 1);
    //        return atoms.ToString();
    //    }
    //}
}
