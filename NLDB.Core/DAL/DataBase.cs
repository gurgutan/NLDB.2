using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;


namespace NLDB.DAL
{
    public class DataBase : IDisposable
    {
        private const int PARALLELIZM = 4;
        private SQLiteConnection db;
        private SQLiteTransaction transaction;
        private Parser[] parsers = null;

        private Dictionary<string, MatrixDictionary> sparseMatrixCash = new Dictionary<string, MatrixDictionary>(SPARSEMATRIX_CASH_SIZE);
        private ConcurrentDictionary<long, AValue> matrixACash = new ConcurrentDictionary<long, AValue>(PARALLELIZM, MATRIXA_CASH_SIZE);
        private ConcurrentDictionary<long, BValue> matrixBCash = new ConcurrentDictionary<long, BValue>(PARALLELIZM, MATRIXB_CASH_SIZE);
        private Dictionary<int, List<Word>> parentsCash = new Dictionary<int, List<Word>>(PARENTS_CASH_SIZE);
        private Dictionary<int, Term> termsCash = new Dictionary<int, Term>(TERMS_CASH_SIZE);
        private Dictionary<int, Word> wordsCash = new Dictionary<int, Word>(WORDS_CASH_SIZE);
        private Dictionary<string, Word> symbolsCash = new Dictionary<string, Word>(SYMBOLS_CASH_SIZE);

        private const int PARENTS_CASH_SIZE = 1 << 18;
        private const int SPARSEMATRIX_CASH_SIZE = 1 << 10;
        private const int MATRIXA_CASH_SIZE = 1 << 20;
        private const int MATRIXB_CASH_SIZE = 1 << 18;
        private const int TERMS_CASH_SIZE = 1 << 20;
        private const int WORDS_CASH_SIZE = 1 << 20;
        private const int SYMBOLS_CASH_SIZE = 1 << 10;


        public Parser[] Parsers
        {
            get
            {
                if (parsers == null)
                    parsers = Splitters().Select(splitter => new Parser(splitter.Expression)).ToArray();
                return parsers;
            }
        }
        //---------------------------------------------------------------------------------------------------------
        //Конструкторы, деструктор
        //---------------------------------------------------------------------------------------------------------
        public DataBase(string databasePath)
        {
            db = new SQLiteConnection($"Data Source={databasePath}; Version=3;");
            db.Open();
            //Create();
        }


        //---------------------------------------------------------------------------------------------------------
        //Создание, инициализация, удаление таблиц
        //---------------------------------------------------------------------------------------------------------
        public void Create()
        {
            var cmd = db.CreateCommand();
            cmd.CommandText =
                "DROP TABLE IF EXISTS Splitters;" +
                "DROP TABLE IF EXISTS Words;" +
                "DROP TABLE IF EXISTS Parents;" +
                "DROP TABLE IF EXISTS MatrixA;" +
                "DROP TABLE IF EXISTS MatrixB;" +
                "CREATE TABLE Splitters (Rank, Expression, PRIMARY KEY(Rank, Expression));" +
                "CREATE TABLE Words (Id INTEGER PRIMARY KEY, Rank INTEGER NOT NULL, Symbol TEXT, Childs TEXT);" +
                "CREATE TABLE Parents (WordId INTEGER NOT NULL, ParentId INTEGER NOT NULL);" +
                "CREATE TABLE MatrixA (Row INTEGER NOT NULL, Column INTEGER NOT NULL, Count INTEGER NOT NULL, Sum REAL NOT NULL, Rank INTEGER NOT NULL, PRIMARY KEY(Row, Column));" +
                "CREATE TABLE MatrixB (Row INTEGER NOT NULL, Column INTEGER NOT NULL, Similarity REAL NOT NULL, Rank INTEGER NOT NULL, PRIMARY KEY(Row, Column));" +
                "CREATE INDEX IWords_symbol ON Words(Symbol); " +
                "CREATE INDEX IWords_childs ON Words(Childs); " +
                "CREATE INDEX IParents_id ON Parents(WordId);" +
                "CREATE INDEX IParents_parentid ON Parents(ParentId);" +
                //"CREATE INDEX IMatrixA_row ON MatrixA(Row);" +
                //"CREATE INDEX IMatrixA_col ON MatrixA(Column);" +
                //"CREATE INDEX IMatrixA_row_col ON MatrixA(Row, Column);" +
                //"CREATE INDEX IMatrixA_rank ON MatrixA(Rank);" +
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
                var reader = cmd.ExecuteReader();
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
            if (GetFromCash(id, out Word word)) return word;
            using (SQLiteCommand cmd = new SQLiteCommand($"SELECT * FROM Words WHERE Id={id};", db))
            {
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                    word = new Word(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3));
            }
            AddToCash(word);
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
            if (GetFromCash(s, out Word word)) return word;
            using (SQLiteCommand cmd = new SQLiteCommand($"SELECT * FROM Words WHERE Symbol=@s LIMIT 1", db))
            {
                cmd.Parameters.AddWithValue("@s", s);
                var reader = cmd.ExecuteReader();
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
            AddToCash(s, word);
            return word;
        }

        //---------------------------------------------------------------------------------------------------------
        //Работа с таблицей Parents
        //---------------------------------------------------------------------------------------------------------
        internal List<Word> GetParents(int wordId)
        {
            //List<Word> result = new List<Word>();
            if (GetFromCash(wordId, out List<Word> result))
                return result;
            using (SQLiteCommand cmd = new SQLiteCommand(
                $"SELECT DISTINCT  Words.Id, Words.Rank, Words.Symbol, Words.Childs, Parents.WordId FROM Words " +
                $"INNER JOIN Parents ON Words.Id=Parents.ParentId WHERE Parents.WordId={wordId};", db))
            {
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    result.Add(new Word(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3)));
                AddToCash(wordId, result);
            }
            return result;
        }

        internal List<Word> GetParents(IEnumerable<int> wordsId)
        {
            List<Word> result = new List<Word>();
            //Фильтруем список id, оставляя только те, которых нет в кэше
            List<int> ids = wordsId.Where(i =>
            {
                if (GetFromCash(i, out List<Word> parents))
                {
                    result.AddRange(parents);
                    return false;
                }
                else
                    return true;
            }).ToList();
            if (ids.Count == 0) return result;
            Dictionary<int, List<Word>> cash = new Dictionary<int, List<Word>>();
            string id_string = string.Join(",", ids);
            using (SQLiteCommand cmd = new SQLiteCommand(
                $"SELECT DISTINCT Words.Id, Words.Rank, Words.Symbol, Words.Childs, Parents.WordId FROM Words " +
                $"INNER JOIN Parents ON Words.Id=Parents.ParentId WHERE Parents.WordId IN ({id_string});", db))
            {
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int id = reader.GetInt32(4);
                    Word w = new Word(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3));
                    result.Add(w);
                    if (!cash.TryGetValue(id, out var plist))
                    {
                        plist = new List<Word>();
                        cash[id] = plist;
                    }
                    plist.Add(w);
                }
            }
            cash.ToList().ForEach(pair => AddToCash(pair.Key, pair.Value));
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
                var reader = await cmd.ExecuteReaderAsync();
                if (!reader.Read()) return default;
                value = new AValue()
                {
                    Rank = rank,
                    R = row,
                    C = column,
                    Count = reader.GetInt32(2),
                    Sum = reader.GetDouble(3)
                };
                AddToCash(value);
                return value;
            }
        }

        public async void SetAValue(int rank, int row, int column, int d)
        {
            var value = GetAValue(row, column, rank).Result;
            string text;
            if (value == null)
            {
                value = new AValue(rank, row, column, d, 1);
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
            AddToCash(value);
        }

        public async void InsertAll(IEnumerable<AValue> values)
        {
            string updateText = $"UPDATE MatrixA SET Count=@cnt, Sum=@sm WHERE Row=@r AND Column=@c;";
            string insertText = $"INSERT INTO MatrixA(Row, Column, Count, Sum, Rank) SELECT @r, @c, @cnt, @sm, @rnk;";
            using (SQLiteCommand cmdUpdate = new SQLiteCommand(updateText, db))
            using (SQLiteCommand cmdInsert = new SQLiteCommand(insertText, db))
            {
                //Параметры команды вставки
                cmdInsert.Parameters.Add("@rnk", System.Data.DbType.Int32);
                cmdInsert.Parameters.Add("@r", System.Data.DbType.Int32);
                cmdInsert.Parameters.Add("@c", System.Data.DbType.Int32);
                cmdInsert.Parameters.Add("@cnt", System.Data.DbType.Int32);
                cmdInsert.Parameters.Add("@sm", System.Data.DbType.Double);
                //Параметры команды замены
                cmdUpdate.Parameters.Add("@r", System.Data.DbType.Int32);
                cmdUpdate.Parameters.Add("@c", System.Data.DbType.Int32);
                cmdUpdate.Parameters.Add("@cnt", System.Data.DbType.Int32);
                cmdUpdate.Parameters.Add("@sm", System.Data.DbType.Double);
                foreach (var v in values)
                {
                    //Поиск в БД значения соответствующего v
                    var value = GetAValue(v.R, v.C, v.Rank).Result;
                    if (value == null)
                    {
                        value = v;
                        cmdInsert.Parameters["@rnk"].Value = value.Rank;
                        cmdInsert.Parameters["@r"].Value = value.R;
                        cmdInsert.Parameters["@c"].Value = value.C;
                        cmdInsert.Parameters["@cnt"].Value = value.Count;
                        cmdInsert.Parameters["@sm"].Value = value.Sum;
                        await cmdInsert.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        value.Count += v.Count;
                        value.Sum += v.Sum;
                        cmdUpdate.Parameters["@r"].Value = value.R;
                        cmdUpdate.Parameters["@c"].Value = value.C;
                        cmdUpdate.Parameters["@cnt"].Value = value.Count;
                        cmdUpdate.Parameters["@sm"].Value = value.Sum;
                        await cmdUpdate.ExecuteNonQueryAsync();
                    }
                    AddToCash(value);
                }
            }
        }

        /// <summary>
        /// Возвращает строки матрицы MatrixA ранга rank с Row из интервала [from,to)
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        public MatrixDictionary GetARows(int from, int to, int rank)
        {
            if (from > to) Swap(ref from, ref to);
            MatrixDictionary rows = new MatrixDictionary();
            string text = $"SELECT Row, Column, Sum, Count AS Value FROM MatrixA " +
                          $"WHERE {from}<=Row AND Row<{to} AND Rank={rank} ";
            using (SQLiteCommand cmd = new SQLiteCommand(text, db))
            {
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int r = reader.GetInt32(0);
                    int c = reader.GetInt32(1);
                    double sum = reader.GetDouble(2);
                    int count = reader.GetInt32(3);
                    if (!rows.TryGetValue(r, out var row))
                    {
                        row = new VectorDictionary();
                        rows[r] = row;
                    }
                    row[c] = sum / c;
                }
                return rows;
            }
        }

        public MatrixDictionary GetARows(IList<Word> words, int rank)
        {
            string rows_ids = string.Join(",", words.Select(w => w.Id.ToString()));
            if (GetFromCash(rows_ids, out MatrixDictionary rows)) return rows;
            rows = new MatrixDictionary();
            string text = $"SELECT Row, Column, Sum, Count AS Value FROM MatrixA " +
                          $"WHERE Row IN ({rows_ids});";
            using (SQLiteCommand cmd = new SQLiteCommand(text, db))
            {
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    AValue value = new AValue(rank: rank, row: reader.GetInt32(0), column: reader.GetInt32(1), sum: reader.GetDouble(2), count: reader.GetInt32(3));
                    if (!rows.TryGetValue(value.R, out var row))
                    {
                        row = new VectorDictionary();
                        rows[value.R] = row;
                    }
                    row[value.C] = value.Mean;
                }
                AddToCash(rows_ids, rows);
                return rows;
            }
        }

        /// <summary>
        /// Заполняет список m координатным представлением матрицы. Возвращает количество строк и столбцов
        /// </summary>
        /// <param name="from">Id слова, с которого начинается выборка</param>
        /// <param name="to">Id конечного слова матрицы</param>
        /// <param name="rank">Ранг слов</param>
        /// <param name="m"></param>
        /// <returns></returns>
        public Tuple<int, int> GetAMatrixAsTuples(int rank, int from, int to, out List<Tuple<int, int, double>> m)
        {
            string text = $"SELECT Row, Column, Sum, Count AS Value FROM MatrixA " +
                          $"WHERE Rank={rank} AND Row>={from} AND Row<{to}";
            m = new List<Tuple<int, int, double>>();
            using (SQLiteCommand cmd = new SQLiteCommand(text, db))
            {
                var reader = cmd.ExecuteReader();
                int rowsCount = 0;
                int columnsCount = 0;
                while (reader.Read())
                {
                    int r = reader.GetInt32(0);
                    int c = reader.GetInt32(1);
                    double sum = reader.GetDouble(2);
                    int count = reader.GetInt32(3);
                    m.Add(Tuple.Create(r, c, sum / count));
                    if (r > rowsCount) rowsCount = r;
                    if (c > columnsCount) columnsCount = c;
                }
                return Tuple.Create(rowsCount + 1, columnsCount + 1);
            }
        }

        internal void Swap(ref int a, ref int b)
        {
            int tmp = a;
            a = b;
            b = tmp;
        }

        //---------------------------------------------------------------------------------------------------------
        //CRUD для MatrixB
        //---------------------------------------------------------------------------------------------------------
        //Row INTEGER NOT NULL, Column INTEGER NOT NULL, Similarity REAL NOT NULL, Rank INTEGER NOT NULL, PRIMARY KEY(Row, Column)
        public async Task<BValue> GetBValue(int row, int column, int rank)
        {
            if (GetFromCash(row, column, out BValue value)) return value;
            using (SQLiteCommand cmd = new SQLiteCommand($"SELECT Row, Column, Similarity, Rank FROM MatrixB WHERE Row={row} AND Column={column} LIMIT 1", db))
            {
                var reader = await cmd.ExecuteReaderAsync();
                if (!reader.Read()) return null;
                return new BValue(rank, row, column, reader.GetInt32(2));
            }
        }

        public async void SetBValue(int rank, int row, int column, int s)
        {
            BValue value = new BValue(rank, row, column, s);
            string text = $"INSERT OR REPLACE INTO MatrixB(Row, Column, Similarity, Rank) SELECT {value.R}, {value.C}, {value.Similarity}, {value.Rank};";
            using (SQLiteCommand cmd = new SQLiteCommand(text, db))
                await cmd.ExecuteNonQueryAsync();
            AddToCash(value);
        }

        public async void InsertAll(IList<BValue> values)
        {
            //Не предполагается изменение значений MatrixB, поэтому только замена: "INSERT OR REPLACE"
            string text = $"INSERT OR REPLACE INTO MatrixB(Row, Column, Similarity, Rank) VALUES (@r, @c, @s, @rnk);";
            using (SQLiteCommand cmd = new SQLiteCommand(text, db))
            {
                cmd.Parameters.Add("@r", System.Data.DbType.Int32);
                cmd.Parameters.Add("@c", System.Data.DbType.Int32);
                cmd.Parameters.Add("@s", System.Data.DbType.Double);
                cmd.Parameters.Add("@rnk", System.Data.DbType.Int32);
                foreach (var v in values)
                {
                    cmd.Parameters["@rnk"].Value = v.Rank;
                    cmd.Parameters["@r"].Value = v.R;
                    cmd.Parameters["@c"].Value = v.C;
                    cmd.Parameters["@s"].Value = v.Similarity;
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public int InsertAll(IEnumerable<Tuple<int, int, double>> values, int rank)
        {
            int count = 0;
            //Не предполагается изменение значений MatrixB, поэтому только замена: "INSERT OR REPLACE"
            string text = $"INSERT OR REPLACE INTO MatrixB(Row, Column, Similarity, Rank) VALUES (@r, @c, @s, @rnk);";
            using (SQLiteCommand cmd = new SQLiteCommand(text, db))
            {
                cmd.Parameters.Add("@r", System.Data.DbType.Int32);
                cmd.Parameters.Add("@c", System.Data.DbType.Int32);
                cmd.Parameters.Add("@s", System.Data.DbType.Double);
                cmd.Parameters.Add("@rnk", System.Data.DbType.Int32);
                foreach (var v in values)
                {
                    cmd.Parameters["@rnk"].Value = rank;
                    cmd.Parameters["@r"].Value = v.Item1;
                    cmd.Parameters["@c"].Value = v.Item2;
                    cmd.Parameters["@s"].Value = v.Item3;
                    count += cmd.ExecuteNonQuery();
                };
            }
            return count;
        }

        public Tuple<int, int> GetBMatrixAsTuples(int rank, int from, int to, out List<Tuple<int, int, double>> m)
        {
            string text = $"SELECT Row, Column, Similarity FROM MatrixB " +
                          $"WHERE Rank={rank} AND Row>={from} AND Row<{to}";
            m = new List<Tuple<int, int, double>>();
            using (SQLiteCommand cmd = new SQLiteCommand(text, db))
            {
                var reader = cmd.ExecuteReader();
                int rowsCount = 0;
                int columnsCount = 0;
                while (reader.Read())
                {
                    int r = reader.GetInt32(0);
                    int c = reader.GetInt32(1);
                    double similarity = reader.GetDouble(2);
                    m.Add(Tuple.Create(r, c, similarity));
                    if (r > rowsCount) rowsCount = r;
                    if (c > columnsCount) columnsCount = c;
                }
                return Tuple.Create(rowsCount, columnsCount);
            }
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
            AddToCash(t);
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

        private bool GetFromCash(int row, int column, out BValue value)
        {
            long key = (((long)row) << 32) | (uint)column;
            return (matrixBCash.TryGetValue(key, out value));
        }

        private void AddToCash(Term t)
        {
            if (t == null) return;
            if (termsCash.Count > TERMS_CASH_SIZE)
                termsCash.Remove(termsCash.Keys.First());
            termsCash[t.id] = t;
        }

        private void AddToCash(Word w)
        {
            if (w == null) return;
            if (wordsCash.Count > WORDS_CASH_SIZE)
                wordsCash.Remove(wordsCash.Keys.First());
            wordsCash[w.Id] = w;
        }

        private void AddToCash(string s, Word w)
        {
            if (w == null) return;
            if (symbolsCash.Count > SYMBOLS_CASH_SIZE)
                symbolsCash.Remove(symbolsCash.Keys.First());
            symbolsCash[s] = w;
        }

        private void AddToCash(AValue value)
        {
            if (value == null) return;
            long key = (((long)value.R) << 32) | (uint)value.C;
            if (matrixACash.Count > MATRIXA_CASH_SIZE)
                RemoveFromCash(matrixACash, 2);
            matrixACash[key] = value;
        }

        private void AddToCash(BValue value)
        {
            if (value == null) return;
            long key = (((long)value.R) << 32) | (uint)value.C;
            if (matrixBCash.Count > MATRIXB_CASH_SIZE)
                RemoveFromCash(matrixBCash, 2);
            matrixBCash[key] = value;
        }

        private void AddToCash(int id, List<Word> parents)
        {
            if (parents == null) return;
            if (parentsCash.Count > PARENTS_CASH_SIZE)
                RemoveFromCash(parentsCash, 2);
            parentsCash[id] = parents;
        }

        private void AddToCash(string key, MatrixDictionary m)
        {
            if (m == null) return;
            if (sparseMatrixCash.Count > SPARSEMATRIX_CASH_SIZE)
                RemoveFromCash(sparseMatrixCash, 2);
            sparseMatrixCash[key] = m;
        }

        private bool GetFromCash(string key, out MatrixDictionary m)
        {
            return (sparseMatrixCash.TryGetValue(key, out m));
        }

        private bool GetFromCash(int key, out List<Word> parents)
        {
            return (parentsCash.TryGetValue(key, out parents));
        }

        private void RemoveFromCash(Dictionary<string, MatrixDictionary> cash, int count)
        {
            int current = 0;
            while (current < count && cash.Count > 0)
                current += cash.Remove(cash.First().Key) ? 1 : 0;
        }

        private void RemoveFromCash(Dictionary<int, List<Word>> cash, int count)
        {
            int current = 0;
            while (current < count && cash.Count > 0)
                current += cash.Remove(cash.First().Key) ? 1 : 0;
        }

        private void RemoveFromCash(ConcurrentDictionary<long, AValue> cash, int count)
        {
            int current = 0;
            while (current < count && cash.Count > 0)
                current += cash.TryRemove(cash.First().Key, out var value) ? 1 : 0;
        }

        private void RemoveFromCash(ConcurrentDictionary<long, BValue> cash, int count)
        {
            int current = 0;
            while (current < count && cash.Count > 0)
                current += cash.TryRemove(cash.First().Key, out var value) ? 1 : 0;
        }

        #region IDisposable Support
        private bool disposedValue = false; // Для определения избыточных вызовов

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    db.Dispose();
                }
                sparseMatrixCash = null;
                matrixACash = null;
                matrixBCash = null;
                termsCash = null;
                wordsCash = null;
                symbolsCash = null;
                parsers = null;

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
