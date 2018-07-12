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
    public partial class Language
    {
        public void DBCreate(string filename)
        {
            string create_info = "DROP TABLE IF EXISTS info; CREATE TABLE info(rank INTEGER NOT NULL, name TEXT NOT NULL, splitters TEXT NOT NULL)";
            string create_word = "DROP TABLE IF EXISTS word; CREATE TABLE word(rank INTEGER NOT NULL, id INTEGER NOT NULL, size INTEGER NOT NULL, name TEXT);";
            string create_childs = "DROP TABLE IF EXISTS childs; CREATE TABLE childs(rank INTEGER NOT NULL, id INTEGER, child INTEGER NOT NULL, pos INTEGER NOT NULL)";
            string create_alphabet = "DROP TABLE IF EXISTS alphabet; CREATE TABLE atoms(id INTEGER, name TEXT NOT NULL)";
            string create_index_words = "CREATE INDEX idx_words ON word(id, rank)";
            string create_index_childs = "CREATE INDEX idx_childs ON childs(id, rank, child)";
            ExecuteNonQuery(filename, create_info + create_word + create_childs + create_alphabet + create_index_words + create_index_childs);
        }

        public void DBSave(string filename)
        {
            //Попытка создать БД
            try
            {
                Console.WriteLine("Создание базы данных");
                DBCreate(filename);
                Console.WriteLine("Добавление общих данных");
                ExecuteNonQuery(filename, InsertInfoCommand());
                Console.WriteLine("Добавление алфавита");
                ExecuteNonQuery(filename, InsertAtomsCommand());
                Console.WriteLine("Добавление слов");
                ExecuteNonQuery(filename, InsertWordsCommand());
                Console.WriteLine("Добавление дочерних связей");
                ExecuteNonQuery(filename, InsertChildsCommand());
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e.Message);
                return;
            }
        }

        public void DBLoad(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException($"Файл {filename} не найден!");
            using (var db = new SQLiteConnection($"Data Source={filename}; Version=3;"))
            {
                db.Open();
                SQLiteCommand cmd_select = db.CreateCommand();
                try
                {
                    LoadWords(db);
                    LoadAlphabet(db);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Ошибка чтения БД " + filename + ". " + e.Message);
                }
                finally
                {
                    db.Close();
                }
            }
        }

        private void LoadAlphabet(SQLiteConnection db)
        {
            throw new NotImplementedException();
        }

        private void LoadWords(SQLiteConnection db)
        {
            string cmd_text = @"SELECT [id],[rank],[size] FROM [words] ORDER BY [id] ASC";

            SQLiteCommand cmd_select = db.CreateCommand();
            cmd_select.CommandText = cmd_text;
            SQLiteDataReader reader = cmd_select.ExecuteReader();
            while (reader.Read())
            {
                Word w = new Word(
                    Convert.ToInt32(reader["id"]),
                    Convert.ToInt32(reader["rank"]),
                    new int[Convert.ToInt32(reader["size"])],
                    null);
                if (reader["name"] != null)
                    alphabet.Add(reader["name"].ToString(), w.id);
            }

            cmd_text = @"SELECT [rank], [id], [child], [pos] FROM [childs] ORDER BY [rank] ASC, [id] ASC, [pos] ASC";
            cmd_select.CommandText = cmd_text;
            reader = cmd_select.ExecuteReader();
            while (reader.Read())
            {
                int id = Convert.ToInt32(reader["id"]);
                int rank = Convert.ToInt32(reader["rank"]);
                int child = Convert.ToInt32(reader["child"]);
                int pos = Convert.ToInt32(reader["pos"]);
                if (Get(id).id == 0) throw new Exception($"Не найдено слово с id={id}");
            }
            reader.Close();

            cmd_select.CommandText = "SELECT * FROM info";
            cmd_select.ExecuteNonQuery();
        }

        private void AddWord(Word w)
        {

        }

        private void SelectInfoCommand(SQLiteConnection db)
        {
            SQLiteCommand cmd_select = db.CreateCommand();
            cmd_select.CommandText = "SELECT rank, name, splitters FROM info";
            cmd_select.ExecuteNonQuery();
        }

        private string InsertInfoCommand()
        {
            string split_symbol = "<:>";
            return $"INSERT INTO info(rank, name, splitters) VALUES(" +
                $"{this.Rank},{this.Name}," +
                this.splitters.Aggregate("", (c, n) => c == "" ? n : $"{c}{split_symbol}{n}") +
                ")";
        }

        //Формирование команд добавления в БД (Insert)
        private string InsertChildsCommand()
        {
            StringBuilder childs = new StringBuilder("INSERT INTO childs(id, rank, child, pos) VALUES");
            StringBuilder values = new StringBuilder();
            foreach (var w in w2i.Keys)
            {
                for (int i = 0; i < w.childs.Length; i++)
                    values.Append($"({w.id},{w.rank},{w.childs[i]},{i}),");
            }
            //Удаляем последнюю запятую
            if (values.Length == 0) return "";
            childs.Append(values.Remove(values.Length - 1, 1));
            return childs.ToString();
        }

        private string InsertWordsCommand()
        {
            if (Words.Count == 0) return "";
            StringBuilder words = new StringBuilder("INSERT INTO word(id, rank, size) VALUES");
            foreach (var w in Words)
            {
                string word = $"({w.Value.id},{w.Value.rank},{w.Value.childs.Length}),";
                words.Append(word);
            }
            //Удаляем последнюю запятую
            words.Remove(words.Length - 1, 1);
            return words.ToString();
        }

        private string InsertAtomsCommand()
        {
            if (this.alphabet.Count == 0) return "";
            StringBuilder letters = new StringBuilder("INSERT INTO alphabet(id, name) VALUES");
            return letters.
                Append(alphabet.Letters.Aggregate("", (c, n) => c == "" ? $"({n.Key},\"{n.Value}\")" : $"{c},({n.Key},\"{n.Value}\")")).
                ToString();
        }

        private void ExecuteNonQuery(string filename, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            using (var db = new SQLiteConnection($"Data Source={filename}; Version=3;"))
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
                    throw new FileNotFoundException("Ошибка создания базы данных " + filename + e.Message);
                }
                finally
                {
                    db.Close();
                }
            }
        }

    }
}
