using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace NLDB.DAL
{
    public class DataBase : IDisposable
    {
        private SQLiteConnection db;
        private SQLiteTransaction transaction;


        public DataBase(string databasePath)
        {
            db = new SQLiteConnection($"Data Source={databasePath}; Version=3;");
            db.Open();
            Create();
        }

        public void Dispose()
        {
            db.Dispose();
        }

        //---------------------------------------------------------------------------------------------------------
        //Создание, инициализация, удаление таблиц
        //---------------------------------------------------------------------------------------------------------
        public void Create()
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText =
                "CREATE TABLE IF NOT EXISTS Splitters (Rank, Expression, PRIMARY KEY(Rank, Expression));" +
                "CREATE TABLE IF NOT EXISTS Words (Id INTEGER PRIMARY KEY, Rank INTEGER NOT NULL, Symbol TEXT, Childs TEXT);" +
                "CREATE TABLE IF NOT EXISTS Parents (WordId INTEGER NOT NULL, ParentId INTEGER NOT NULL);" +
                "CREATE TABLE IF NOT EXISTS MatrixA (Row INTEGER NOT NULL, Column INTEGER NOT NULL, Count INTEGER NOT NULL, Sum REAL NOT NULL, Rank INTEGER NOT NULL, PRIMARY KEY(Row, Column));" +
                "CREATE TABLE IF NOT EXISTS MatrixB (Row INTEGER NOT NULL, Column INTEGER NOT NULL, Similarity REAL NOT NULL, Rank INTEGER NOT NULL, PRIMARY KEY(Row, Column));" +
                "DROP INDEX IF EXISTS IWords_childs;" +
                "DROP INDEX IF EXISTS IParents_id;" +
                "DROP INDEX IF EXISTS IParents_parentid;" +
                "DROP INDEX IF EXISTS IMatrixA_row;" +
                "DROP INDEX IF EXISTS IMatrixA_col;" +
                "DROP INDEX IF EXISTS IMatrixA_row_col;" +
                "DROP INDEX IF EXISTS IMatrixA_rank;" +
                "DROP INDEX IF EXISTS IMatrixB_row_col;" +
                "CREATE INDEX IWords_childs ON Words(Childs); " +
                "CREATE INDEX IParents_id ON Parents(WordId);" +
                "CREATE INDEX IParents_parentid ON Parents(ParentId);" +
                "CREATE INDEX IMatrixA_row ON MatrixA(Row);" +
                "CREATE INDEX IMatrixA_col ON MatrixA(Column);" +
                "CREATE INDEX IMatrixA_row_col ON MatrixA(Row, Column);" +
                "CREATE INDEX IMatrixA_rank ON MatrixA(Rank);" +
                "CREATE INDEX IMatrixB_row_col ON MatrixB(Row, Column);";
            cmd.ExecuteNonQuery();
        }

        public void BeginTransaction()
        {
            transaction = db.BeginTransaction();
        }

        public void Commit()
        {
            transaction.Commit();
            transaction = null;
        }

        public void Rollback()
        {
            transaction.Rollback();
            transaction = null;
        }

        //---------------------------------------------------------------------------------------------------------
        //Удаление данных из таблиц
        //---------------------------------------------------------------------------------------------------------
        public void ClearAll()
        {
            ExecuteCommand("DELETE FROM Splitters");
            ExecuteCommand("DELETE FROM Words");
            ExecuteCommand("DELETE FROM Parents");
            ExecuteCommand("DELETE FROM MatrixA");
            ExecuteCommand("DELETE FROM MatrixB");
            currentId = 0;
        }

        public void ClearMatrixA()
        {
            ExecuteCommand("DELETE FROM MatrixA");
        }
        public void ClearMatrixB()
        {
            ExecuteCommand("DELETE FROM MatrixB");
        }
        public void ClearSplitters()
        {
            ExecuteCommand("DELETE FROM Splitters");
        }

        //---------------------------------------------------------------------------------------------------------
        //Работа с Id слов
        //---------------------------------------------------------------------------------------------------------
        public int NewId()
        {
            currentId = CurrentId + 1;
            return currentId;
        }

        public int GetMaxId()
        {
            int? id;
            using (SQLiteCommand cmd = new SQLiteCommand($"SELECT MAX(Id) FROM Words;", db))
                id = cmd.ExecuteScalar() as int?;
            return (id == null) ? 0 : (int)id;
        }

        private int currentId;
        private int CurrentId
        {
            get
            {
                if (currentId == 0) currentId = GetMaxId();
                return currentId;
            }
        }

        //---------------------------------------------------------------------------------------------------------
        //CRUD для Splitters
        //---------------------------------------------------------------------------------------------------------
        public IList<Splitter> Splitters()
        {
            List<Splitter> result = new List<Splitter>();
            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Splitters ORDER BY Rank;", db))
            {
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    result.Add(new Splitter(reader.GetInt32(0), reader.GetString(1)));
            }
            return result;
        }

        public void Insert(Splitter s)
        {
            ExecuteCommand($"INSERT INTO Splitters(Rank, Expression) VALUES({s.Rank}, '{s.Expression}');");
        }

        //---------------------------------------------------------------------------------------------------------
        //CRUD для Words
        //---------------------------------------------------------------------------------------------------------
        public void Insert(Parent p)
        {
            ExecuteCommand($"INSERT INTO Parents(WordId, ParentId) VALUES ({p.WordId},{p.ParentId})");
        }

        public void InsertAll(IEnumerable<Parent> parents)
        {
            string text = "INSERT INTO Parents(WordId, ParentId) VALUES (@w,@p);";
            using (SQLiteCommand cmd = new SQLiteCommand(text, db))
            {
                foreach (var p in parents)
                {
                    cmd.Parameters.AddWithValue("@w", p.WordId);
                    cmd.Parameters.AddWithValue("@p", p.ParentId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------
        //CRUD для Words
        //---------------------------------------------------------------------------------------------------------
        public IList<Word> Words(string where = "1")
        {
            List<Word> result = new List<Word>();
            using (SQLiteCommand cmd = new SQLiteCommand($"SELECT * FROM Words WHERE {where};", db))
            {
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    result.Add(new Word(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3)));
            }
            return result;
        }

        public Word GetWord(int id)
        {
            using (SQLiteCommand cmd = new SQLiteCommand($"SELECT * FROM Words WHERE Id={id};", db))
            {
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                    return new Word(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3));
            }
            return null;
        }

        public int Add(Word w)
        {
            w.Id = NewId();
            Insert(w);
            InsertAll(w.ChildsId.Select(i => new Parent(w.Id, i)));
            return w.Id;
        }

        public void Insert(Word w)
        {
            ExecuteCommand($"INSERT INTO Words(Id, Rank, Symbol, Childs) VALUES({w.Id}, {w.Rank}, '{w.Symbol}', '{w.Childs}');");
        }

        public void InsertAll(IEnumerable<Word> words)
        {
            string text = "INSERT INTO Words(Id, Rank, Symbol, Childs) VALUES (@a,@b,@c,@d);";
            using (SQLiteCommand cmd = new SQLiteCommand(text, db))
            {
                foreach (var w in words)
                {
                    cmd.Parameters.AddWithValue("@a", w.Id);
                    cmd.Parameters.AddWithValue("@b", w.Rank);
                    cmd.Parameters.AddWithValue("@c", w.Symbol);
                    cmd.Parameters.AddWithValue("@d", w.Childs);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        internal Word GetWordByChilds(string childs)
        {
            using (SQLiteCommand cmd = new SQLiteCommand($"SELECT * FROM Words WHERE Childs='{childs}' LIMIT 1", db))
            {
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new Word()
                    {
                        Id = reader.GetInt32(0),
                        Rank = reader.GetInt32(1),
                        Symbol = reader.GetString(2),
                        Childs = reader.GetString(3)
                    };
                }
            }
            return null;
        }

        internal Word GetWordBySymbol(string s)
        {
            using (SQLiteCommand cmd = new SQLiteCommand($"SELECT * FROM Words WHERE Symbol='{s}' LIMIT 1", db))
            {
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new Word()
                    {
                        Id = reader.GetInt32(0),
                        Rank = reader.GetInt32(1),
                        Symbol = reader.GetString(2),
                        Childs = reader.GetString(3)
                    };
                }
            }
            return null;
        }

        //---------------------------------------------------------------------------------------------------------
        //Универсальные CRUD 
        //---------------------------------------------------------------------------------------------------------
        private void ExecuteCommand(string text)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(text, db))
                cmd.ExecuteNonQuery();
        }

        private void ExecuteCommand(string text, params Tuple<string, object>[] parameters)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(text, db))
            {
                Array.ForEach(parameters, p => cmd.Parameters.AddWithValue(p.Item1, p.Item2));
                cmd.ExecuteNonQuery();
            }
        }
    }
}
