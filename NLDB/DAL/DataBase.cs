using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace NLDB.DAL
{
    public class DataBase : IDisposable
    {
        private SQLiteConnection db;
        private SQLiteTransaction transaction;
        private Parser[] parsers = null;
        public Parser[] Parsers
        {
            get
            {
                if (parsers == null)
                    parsers = Splitters().Select(splitter => new Parser(splitter.Expression)).ToArray();
                return parsers;
            }
        }

        private readonly Dictionary<long, AValue> matrixACash = new Dictionary<long, AValue>(MATRIXA_CASH_SIZE);
        private readonly Dictionary<int, Term> termsCash = new Dictionary<int, Term>(TERMS_CASH_SIZE);
        private readonly Dictionary<int, Word> wordsCash = new Dictionary<int, Word>(WORDS_CASH_SIZE);
        private readonly Dictionary<string, Word> symbolsCash = new Dictionary<string, Word>(SYMBOLS_CASH_SIZE);

        private const int MATRIXA_CASH_SIZE = 1 << 18;
        private const int TERMS_CASH_SIZE = 1 << 18;
        private const int WORDS_CASH_SIZE = 1 << 18;
        private const int SYMBOLS_CASH_SIZE = 1 << 10;


        //---------------------------------------------------------------------------------------------------------
        //Конструкторы, деструктор
        //---------------------------------------------------------------------------------------------------------
        public DataBase(string databasePath)
        {
            db = new SQLiteConnection($"Data Source={databasePath}; Version=3;");
            db.Open();
            //Create();
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
                "DROP TABLE IF EXISTS Splitters;" +
                "DROP TABLE IF EXISTS Words;" +
                "DROP TABLE IF EXISTS Parents;" +
                "DROP TABLE IF EXISTS MatrixA;" +
                "DROP TABLE IF EXISTS MatrixB;" +
                "CREATE TABLE NOT IF EXISTS Splitters (Rank, Expression, PRIMARY KEY(Rank, Expression));" +
                "CREATE TABLE NOT IF EXISTS Words (Id INTEGER PRIMARY KEY, Rank INTEGER NOT NULL, Symbol TEXT, Childs TEXT);" +
                "CREATE TABLE NOT IF EXISTS Parents (WordId INTEGER NOT NULL, ParentId INTEGER NOT NULL);" +
                "CREATE TABLE NOT IF EXISTS MatrixA (Row INTEGER NOT NULL, Column INTEGER NOT NULL, Count INTEGER NOT NULL, Sum INTEGER NOT NULL, Rank INTEGER NOT NULL, PRIMARY KEY(Row, Column));" +
                "CREATE TABLE NOT IF EXISTS MatrixB (Row INTEGER NOT NULL, Column INTEGER NOT NULL, Similarity REAL NOT NULL, Rank INTEGER NOT NULL, PRIMARY KEY(Row, Column));" +
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
        public void Clear(string tableName)
        {
            ExecuteCommand($"DELETE FROM {tableName}");
            parsers = null;
        }

        public void ClearAll()
        {
            ExecuteCommand("DELETE FROM Splitters");
            ExecuteCommand("DELETE FROM Words");
            ExecuteCommand("DELETE FROM Parents");
            ExecuteCommand("DELETE FROM MatrixA");
            ExecuteCommand("DELETE FROM MatrixB");
            currentId = 0;
            parsers = null;
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

        public int MaxRank => Parsers.Length - 1;

        //---------------------------------------------------------------------------------------------------------
        //CRUD для Splitters
        //---------------------------------------------------------------------------------------------------------
        public IList<Splitter> Splitters()
        {
            List<Splitter> result = new List<Splitter>();
            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Splitters ORDER BY Rank;", db))
            {
                SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                    result.Add(new Splitter(reader.GetInt32(0), reader.GetString(1)));
            }
            return result;
        }

        public void Insert(Splitter s)
        {
            ExecuteCommand($"INSERT INTO Splitters(Rank, Expression) VALUES({s.Rank}, '{s.Expression}');");
            parsers = null;
        }

        //---------------------------------------------------------------------------------------------------------
        //CRUD для Parents
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
                foreach (Parent p in parents)
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
                SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                    result.Add(new Word(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3)));
            }
            return result;
        }

        public Word GetWord(int id)
        {
            if (GetFromCash(id, out Word word)) return word;
            using (SQLiteCommand cmd = new SQLiteCommand($"SELECT * FROM Words WHERE Id={id};", db))
            {
                SQLiteDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                    word = new Word(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3));
            }
            SaveToCash(word);
            return word;
        }

        public int Add(Word w)
        {
            w.Id = NewId();
            Insert(w);
            InsertAll(w.ChildsId.Select(i => new Parent(i, w.Id)));
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
                foreach (Word w in words)
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
                SQLiteDataReader reader = cmd.ExecuteReader();
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
            if (GetFromCash(s, out Word word)) return word;
            using (SQLiteCommand cmd = new SQLiteCommand($"SELECT * FROM Words WHERE Symbol=@s LIMIT 1", db))
            {
                cmd.Parameters.AddWithValue("@s", s);
                SQLiteDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    word = new Word()
                    {
                        Id = reader.GetInt32(0),
                        Rank = reader.GetInt32(1),
                        Symbol = reader.GetString(2),
                        Childs = reader.GetString(3)
                    };
                }
            }
            SaveToCash(s, word);
            return word;
        }

        //---------------------------------------------------------------------------------------------------------
        //Работа с таблицей Parents
        //---------------------------------------------------------------------------------------------------------
        internal List<Word> GetParents(int wordId)
        {
            List<Word> result = new List<Word>();
            using (SQLiteCommand cmd = new SQLiteCommand($"SELECT * FROM Words INNER JOIN Parents ON Words.Id=Parents.ParentId WHERE Parents.WordId={wordId};", db))
            {
                SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                    result.Add(new Word(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3)));
            }
            return result;
        }

        internal List<Word> GetParents(IEnumerable<int> wordsId)
        {
            string ids = string.Join(",", wordsId);
            List<Word> result = new List<Word>();
            using (SQLiteCommand cmd = new SQLiteCommand($"SELECT * FROM Words INNER JOIN Parents ON Words.Id=Parents.ParentId WHERE Parents.WordId IN ({ids});", db))
            {
                SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                    result.Add(new Word(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3)));
            }
            return result;
        }

        //---------------------------------------------------------------------------------------------------------
        //CRUD для MatrixA
        //---------------------------------------------------------------------------------------------------------
        private void InsertAsync(AValue v)
        {
            ExecuteCommandAsync($"INSERT INTO MatrixA(Row, Column, Count, Sum, Rank) VALUES({v.R}, {v.C}, {v.Count}, {v.Sum}, {v.Rank});");
        }

        private void UpdateAsync(AValue v)
        {
            ExecuteCommandAsync($"UPDATE MatrixA SET Count={v.Count}, Sum={v.Sum} WHERE Row={v.R} AND Column={v.C};");
        }

        public async Task<AValue> GetAValue(int row, int column, int rank)
        {
            if (GetFromCash(row, column, out AValue value)) return value;
            using (SQLiteCommand cmd = new SQLiteCommand($"SELECT Row, Column, Count, Sum FROM MatrixA WHERE Row={row} AND Column={column} LIMIT 1", db))
            {
                DbDataReader reader = await cmd.ExecuteReaderAsync();
                if (!reader.Read()) return null;
                return new AValue()
                {
                    Rank = rank,
                    R = row,
                    C = column,
                    Count = reader.GetInt32(2),
                    Sum = reader.GetInt32(3)
                };
            }
        }

        public async void SetAValue(int rank, int row, int column, int d)
        {
            AValue value = GetAValue(row, column, rank).Result;
            string text;
            if (value == null)
            {
                text = $"INSERT INTO MatrixA(Row, Column, Count, Sum, Rank) SELECT {row}, {column}, 1, {d}, {rank};";
            }
            else
            {
                value.Sum += d;
                value.Count++;
                text = $"UPDATE MatrixA SET Count={value.Count}, Sum={value.Sum} WHERE Row={row} AND Column={column};";
            }
            using (SQLiteCommand cmd = new SQLiteCommand(text, db))
                await cmd.ExecuteNonQueryAsync();
            SaveToCash(value);
        }


        //---------------------------------------------------------------------------------------------------------
        //Универсальные CRUD 
        //---------------------------------------------------------------------------------------------------------
        private void ExecuteCommand(string text)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(text, db))
                cmd.ExecuteNonQuery();
        }

        private async void ExecuteCommandAsync(string text)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(text, db))
                await cmd.ExecuteNonQueryAsync();
        }

        private void ExecuteCommand(string text, params Tuple<string, object>[] parameters)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(text, db))
            {
                Array.ForEach(parameters, p => cmd.Parameters.AddWithValue(p.Item1, p.Item2));
                cmd.ExecuteNonQuery();
            }
        }

        //---------------------------------------------------------------------------------------------------------
        //Методы для работы с Термами
        //---------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Преобразование Слова в Терм
        /// </summary>
        /// <param name="w"></param>
        /// <param name="confidence"></param>
        /// <returns></returns>
        public Term ToTerm(Word w, float confidence = 1)
        {
            if (w == null) return null;
            if (GetFromCash(w.Id, out Term t)) return t;
            t = new Term(
                w.Rank,
                w.Id,
                _confidence: confidence,
                _text: w.Symbol,
                _childs: w.Rank == 0 ? null : w.ChildsId.Select(c => ToTerm(GetWord(c))));
            SaveToCash(t);
            return t;
        }

        public Term ToTerm(string text, int rank)
        {
            text = Parser.Normilize(text);
            return new DAL.Term(rank, 0, 0, text,
                rank == 0 ? null :
                Parsers[rank - 1].
                Split(text).
                Where(s => !string.IsNullOrWhiteSpace(s)).
                Select(s => ToTerm(s, rank - 1)));
        }

        //---------------------------------------------------------------------------------------------------------
        //Методы для работы с парсерами
        //---------------------------------------------------------------------------------------------------------
        internal string[] Split(string text, int rank = -1)
        {
            if (rank == -1) rank = MaxRank;
            return Parsers[rank].Split(text);
        }

        //---------------------------------------------------------------------------------------------------------
        //Методы работы с кэшем
        //---------------------------------------------------------------------------------------------------------
        private bool GetFromCash(int id, out Word result)
        {
            return wordsCash.TryGetValue(id, out result);
        }

        private bool GetFromCash(string symbol, out Word result)
        {
            return symbolsCash.TryGetValue(symbol, out result);
        }

        private bool GetFromCash(int id, out Term result)
        {
            return termsCash.TryGetValue(id, out result);
        }

        private bool GetFromCash(int row, int column, out AValue value)
        {
            long key = (((long)row) << 32) | (uint)column;
            return (matrixACash.TryGetValue(key, out value));
        }

        private void SaveToCash(Term t)
        {
            if (t == null) return;
            if (termsCash.Count > TERMS_CASH_SIZE)
                termsCash.Remove(termsCash.Keys.First());
            termsCash[t.id] = t;
        }

        private void SaveToCash(Word w)
        {
            if (w == null) return;
            if (wordsCash.Count > WORDS_CASH_SIZE)
                wordsCash.Remove(wordsCash.Keys.First());
            wordsCash[w.Id] = w;
        }

        private void SaveToCash(string s, Word w)
        {
            if (w == null) return;
            if (symbolsCash.Count > SYMBOLS_CASH_SIZE)
                symbolsCash.Remove(symbolsCash.Keys.First());
            symbolsCash[s] = w;
        }

        private void SaveToCash(AValue value)
        {
            if (value == null) return;
            long key = (((long)value.R) << 32) | (uint)value.C;
            if (matrixACash.Count > MATRIXA_CASH_SIZE)
                matrixACash.Remove(matrixACash.Keys.First());
            matrixACash[key] = value;
        }
    }
}
