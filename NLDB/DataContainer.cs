using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace NLDB
{
    /// <summary>
    /// Класс для работы данными. Реализует работу со Словами лексикона, скрывая подробности работы с базой данных SQLite
    /// </summary>
    public class DataContainer : IEnumerable<Word>, IDisposable
    {
        private const int max_similars_per_word = 256;
        private SQLiteTransaction transaction;
        private SQLiteConnection db;

        //public DMatrix dmatrix = new DMatrix();

        //Кэш термов для быстрого выполнения метода ToTerm
        private readonly Dictionary<int, Term_old> terms = new Dictionary<int, Term_old>(1 << 18);

        //Кэш символов алфавита
        private readonly Dictionary<string, int> alphabet = new Dictionary<string, int>(1 << 10);

        //Кэш id слов для поиска по childs
        private readonly Dictionary<Sequence, int> words_id = new Dictionary<Sequence, int>(1 << 10);

        private int current_id = 0;

        private string[] splitters;
        public string[] Splitters
        {
            get => splitters;
            set => splitters = value;
        }

        private string dbname = "data.db";

        public DataContainer(string _dbname, string[] _splitters)
        {
            dbname = _dbname;
            splitters = _splitters;
            current_id = 0;
        }

        public DataContainer(string _dbname)
        {
            dbname = _dbname;
            splitters = null;
            current_id = 0;
        }

        //--------------------------------------------------------------------------------------------
        //Работа с базой данных SQLite
        //--------------------------------------------------------------------------------------------
        public bool IsConnected()
        {
            if (db == null) return false;
            return db.State == System.Data.ConnectionState.Open;
        }

        public SQLiteConnection Connect(string _dbname)
        {
            //if (db.State == System.Data.ConnectionState.Open) return db;
            //TODO: переделать для безопасного использования (try catch и т.п.)
            if (!File.Exists(_dbname))
            {
                throw new FileNotFoundException($"База данных не найдена по пути '{_dbname}'");
            }
            dbname = _dbname;
            db = new SQLiteConnection($"Data Source={dbname}; Version=3;");
            db.Open();
            current_id = CurrentId;
            ReadSplitters();
            CreateCash();
            return db;
        }

        public void Disconnect()
        {
            if (db.State == System.Data.ConnectionState.Open)
                db.Close();
        }

        public void CreateDB()
        {
            current_id = 0;
            if (IsConnected()) Disconnect();                        // Закрываем
            if (File.Exists(dbname)) File.Delete(dbname); // Удаляем БД
            db = new SQLiteConnection($"Data Source={dbname}; Version=3;");  // Создаем новую БД
            db.Open();     // Открываем соединение
            //Создаем таблицы
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText =
                "CREATE TABLE splitters (rank, expr);" +
                "CREATE TABLE words (id INTEGER PRIMARY KEY, rank INTEGER NOT NULL, symbol, childs);" +
                "CREATE TABLE parents (id INTEGER NOT NULL, parent_id INTEGER NOT NULL);" +
                "CREATE TABLE dmatrix (" +
                    "row INTEGER NOT NULL, " +
                    "column integer NOT NULL, " +
                    "count INTEGER NOT NULL, " +
                    "sum REAL NOT NULL, " +
                    "rank INTEGER NOT NULL, " +
                    "PRIMARY KEY(row, column));" +
                "CREATE TABLE smatrix (" +
                    "row INTEGER NOT NULL, " +
                    "column integer NOT NULL, " +
                    "similarity REAL NOT NULL, " +
                    "rank INTEGER NOT NULL, " +
                    "PRIMARY KEY(row, column));";
            //+"CREATE TABLE grammar (id, next INTEGER NOT NULL, pos INTEGER NOT NULL, count INTEGER NOT NULL );";
            cmd.ExecuteNonQuery();
            //Добавляем разделители слов в таблицу splitters
            string splitters_values = splitters.
                Select((s, i) => $"({i},'{s}')").
                Aggregate("", (c, n) => c + (c == "" ? "" : ",") + n);
            cmd.CommandText = $"INSERT INTO splitters (rank, expr) VALUES {splitters_values}";
            cmd.ExecuteNonQuery();
            //Создаем индексы
            cmd.CommandText =
                "CREATE INDEX childs_ind ON words (childs);"
                + "CREATE INDEX parents_id_ind ON parents (id);"
                + "CREATE INDEX parents_p_id_ind ON parents (parent_id);"
                + "CREATE INDEX words_id_ind ON words (id);"
                + "CREATE INDEX dmatrix_row_ind ON dmatrix (row);"
                + "CREATE INDEX dmatrix_col_ind ON dmatrix (column);"
                + "CREATE INDEX dmatrix_row_col_ind ON dmatrix (row, column);"
                + "CREATE INDEX smatrix_row_col_ind ON smatrix (row, column);"
                + "CREATE INDEX dmatrix_rank_ind ON dmatrix (rank);";
            //+ "CREATE INDEX smatrix_rank_ind ON smatrix (rank);";
            //+"CREATE INDEX grammar_id_ind ON grammar (id);" 
            //+"CREATE INDEX grammar_id_next_pos_ind ON grammar (id, next ASC, pos ASC);";
            cmd.ExecuteNonQuery();
            db.Close();
        }

        public void BeginTransaction()
        {
            transaction = db.BeginTransaction();
        }

        public void EndTransaction()
        {
            transaction.Commit();
            transaction = null;
        }

        public bool IsTransaction()
        {
            return (transaction != null);
        }

        public void Commit()
        {
            if (IsTransaction()) EndTransaction();
            BeginTransaction();
        }

        public void StartSession()
        {
            if (!IsConnected())
                throw new Exception($"Подключение к БД не установлено");
            transaction = db.BeginTransaction();
        }

        public void EndSession()
        {
            if (IsTransaction()) EndTransaction();
        }

        /// <summary>
        /// Возвращает количество слов в базе данных
        /// </summary>
        /// <returns></returns>
        public int CountWords()
        {
            return int.Parse(ExecuteScalar($"SELECT COUNT(*) FROM words;").ToString());
        }

        public int Max(string tablename, string column)
        {
            string str = ExecuteScalar($"SELECT MAX(cast({column} as INTEGER)) FROM {tablename};").ToString();
            return str == "" ? 0 : int.Parse(str);
        }

        //--------------------------------------------------------------------------------------------
        //Преобразование Слова в Терм
        //--------------------------------------------------------------------------------------------
        public Term_old ToTerm(Word w, float confidence = 1)
        {
            //if (this.terms.TryGetValue(w.id, out Term t)) return t;
            if (w == null) return null;
            Term_old t = new Term_old(
                w.rank,
                w.id,
                _confidence: confidence,
                _text: w.symbol,
                _childs: w.rank == 0 ? null : w.childs.Select(c => ToTerm(c)));
            //Сохраняем в кэш
            terms[w.id] = t;
            return t;
        }

        public Term_old ToTerm(int i, float confidence = 1)
        {
            if (terms.TryGetValue(i, out Term_old t))
                t.confidence = confidence;
            else
                t = ToTerm(GetWord(i));
            return t;
        }

        public IEnumerable<Term_old> ToTerms(IEnumerable<int> ids)
        {
            return GetWords(ids).Select(w => ToTerm(w));
        }

        internal void ReadSplitters()
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = $"SELECT rank, expr FROM splitters;";
            SQLiteDataReader reader = cmd.ExecuteReader();
            List<Tuple<int, string>> db_splitters = new List<Tuple<int, string>>();
            while (reader.Read())
            {
                int rank = reader.GetInt32(0);
                string expr = reader.GetString(1);
                db_splitters.Add(new Tuple<int, string>(rank, expr));
            }
            Splitters = db_splitters.OrderBy(t => t.Item1).Select(t => t.Item2).ToArray();
        }
        //--------------------------------------------------------------------------------------------
        //Работа с матрицей расстояний
        //--------------------------------------------------------------------------------------------
        public void DMatrixClear()
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = $"DELETE FROM dmatrix";
            cmd.ExecuteNonQuery();
        }

        public bool DMatrixContainsRow(int r)
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = $"SELECT row,column FROM dmatrix WHERE row={r} LIMIT 1;";
            object result = cmd.ExecuteScalar();
            return result != null;
        }

        public bool DMatrixContains(int r, int c)
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = $"SELECT row,column FROM dmatrix WHERE row={r} and column={c} LIMIT 1;";
            object result = cmd.ExecuteScalar();
            return result != null;
        }

        public Dictionary<int, MeanValue> DMatrixGetRow(int r)
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = $"SELECT row, column, count, sum FROM dmatrix WHERE row={r};";
            SQLiteDataReader reader = cmd.ExecuteReader();
            Dictionary<int, MeanValue> row = new Dictionary<int, MeanValue>();
            while (reader.Read())
            {
                int c = reader.GetInt32(1);
                int count = reader.GetInt32(2);
                float sum = reader.GetFloat(3);
                MeanValue info = new MeanValue(count, sum);
                row.Add(c, info);
            }
            return row;
        }

        public MeanValue DMatrixGetValue(int r, int c)
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = $"SELECT row, column, count, sum FROM dmatrix WHERE row={r} and column={c} LIMIT 1;";
            SQLiteDataReader reader = cmd.ExecuteReader();
            if (!reader.Read()) return new MeanValue(-1, 0);
            int count = reader.GetInt32(2);
            float sum = reader.GetFloat(3);
            return new MeanValue(count, sum);
        }

        public async void DMatrixAddValue(int r, int c, float s, int rank)
        {
            MeanValue value = DMatrixGetValue(r, c);
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = value.count == -1
                ? $"INSERT INTO dmatrix(row, column, count, sum, rank) SELECT {r}, {c}, 1, {s.ToString()}, {rank};"
                : $"UPDATE dmatrix SET count={value.count + 1}, sum={value.sum + s} WHERE row={r} AND column={c};";
            await cmd.ExecuteNonQueryAsync();
        }

        public Pointer DMatrixRowMin(int r)
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText =
                $"SELECT row, column, count, sum, sum/count as avgval FROM dmatrix " +
                $"WHERE row={r} and avgval = (SELECT MIN(sum/count) FROM dmatrix WHERE row={r});";
            SQLiteDataReader reader = cmd.ExecuteReader();
            if (!reader.Read()) return new Pointer();
            int column = reader.GetInt32(1);
            int count = reader.GetInt32(2);
            float sum = reader.GetFloat(3);
            return new Pointer(column, count, sum);
        }

        public Pointer DMatrixRowMax(int r)
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText =
                $"SELECT row, column, count, sum, sum/count as avgval FROM dmatrix " +
                $"WHERE row={r} and avgval = (SELECT MAX(sum/count) WHERE row={r} FROM dmatrix)";
            SQLiteDataReader reader = cmd.ExecuteReader();
            if (!reader.Read()) return new Pointer();
            int column = reader.GetInt32(1);
            int count = reader.GetInt32(2);
            float sum = reader.GetFloat(3);
            return new Pointer(column, count, sum);
        }

        public Dictionary<int, Dictionary<int, float>> DMatrixGetRows(int from, int to, int rank)
        {
            if (from > to)
            {
                int tmp = from;
                from = to;
                to = tmp;
            }
            Dictionary<int, Dictionary<int, float>> rows = new Dictionary<int, Dictionary<int, float>>(1 << 10);
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText =
                $"SELECT row, column, sum/count AS value FROM dmatrix " +
                $"WHERE {from}<=row AND row<{to} AND rank={rank} ";
            SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int r = reader.GetInt32(0);
                int c = reader.GetInt32(1);
                float v = reader.GetFloat(2);
                if (!rows.TryGetValue(r, out Dictionary<int, float> row))
                {
                    row = new Dictionary<int, float>();
                    rows[r] = row;
                }
                row[c] = v;
            }
            return rows;
        }

        //--------------------------------------------------------------------------------------------
        //Матрица подобия слов
        //--------------------------------------------------------------------------------------------
        public void SMatrixClear()
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = $"DELETE FROM smatrix";
            cmd.ExecuteNonQuery();
        }

        public float SMatrixGetValue(int r, int c)
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = $"SELECT similarity FROM smatrix WHERE row={r} and column={c} LIMIT 1;";
            object result = cmd.ExecuteScalar();
            return (float)result;
        }

        public float SMatrixSetValue(int r, int c, float s, int rank)
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText =
                $"DELETE FROM smatrix WHERE row={r} and column={c};" +
                $"INSERT INTO smatrix(row, column, similarity, rank) VALUES({r}, {c}, {s.ToString().Replace(',', '.')},{rank});";
            cmd.ExecuteNonQueryAsync();
            return s;
        }


        internal async void SMatrixSetValue(List<Tuple<int, int, int, float>> row)
        {
            using (SQLiteCommand cmd = db.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO smatrix(row, column, similarity, rank) VALUES(@r, @c, @s, @rnk);";
                cmd.Parameters.AddWithValue("@r", 0);
                cmd.Parameters.AddWithValue("@c", 0);
                cmd.Parameters.AddWithValue("@s", 0.0);
                cmd.Parameters.AddWithValue("@rnk", 0);
                foreach (Tuple<int, int, int, float> quadriple in row)
                {
                    cmd.Parameters["@r"].Value = quadriple.Item1;
                    cmd.Parameters["@c"].Value = quadriple.Item2;
                    cmd.Parameters["@s"].Value = quadriple.Item4;
                    cmd.Parameters["@rnk"].Value = quadriple.Item3;
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public Dictionary<int, float> SMatrixGetRow(int r)
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = $"SELECT row, column, similarity FROM smatrix WHERE row={r} ORDER BY similarity ASC;";
            SQLiteDataReader reader = cmd.ExecuteReader();
            Dictionary<int, float> result = new Dictionary<int, float>();
            while (reader.Read())
            {
                int id = reader.GetInt32(1);
                float s = reader.GetFloat(2);
                result[id] = s;
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r">номер строки (id слова)</param>
        /// <param name="count">количество элементов вектора для возврата; если 0, то вернуть все</param>
        /// <returns></returns>
        public Dictionary<int, float> SMatrixGetRow(int r, int count = 0)
        {
            SQLiteCommand cmd = db.CreateCommand();
            if (count == 0)
                cmd.CommandText = $"SELECT column, similarity FROM smatrix WHERE row={r} ORDER BY similarity ASC;";
            else
                cmd.CommandText = $"SELECT column, similarity FROM smatrix WHERE row={r} ORDER BY similarity ASC LIMIT {count};";
            SQLiteDataReader reader = cmd.ExecuteReader();
            Dictionary<int, float> result = new Dictionary<int, float>();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                float s = reader.GetFloat(1);
                result[id] = s;
            }
            return result;
        }

        public async void SMatrixCalcTable(IEnumerable<int> rows)
        {
            int rowsCount = rows.Count();
            string rows_str = rows.Aggregate("", (c, n) => c == "" ? n.ToString() : c + "," + n.ToString());
            //Расчет расстояния L1 для строк матрицы rows и вставка в таблицу smatrix
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText =
                $"INSERT INTO smatrix(row, column, similarity, rank) " +
                $"SELECT r, c, s, rnk FROM (" +
                $"SELECT dmatrix1.row AS r, dmatrix2.row AS c, SUM(ABS(dmatrix1.sum/dmatrix1.count-dmatrix2.sum/dmatrix2.count)) AS s, dmatrix1.rank AS rnk " +
                $"FROM dmatrix dmatrix1 INNER JOIN dmatrix dmatrix2 ON dmatrix1.column=dmatrix2.column AND dmatrix1.row<dmatrix2.row " +
                $"WHERE dmatrix1.row IN ({rows_str}) " +
                $"GROUP BY r, c " +
                $"ORDER BY r, s " +
                $"LIMIT {max_similars_per_word * rowsCount}) " +
                $"WHERE r NOT NULL AND c NOT NULL AND s NOT NULL AND rnk NOT NULL;";
            await cmd.ExecuteNonQueryAsync();
        }


        //--------------------------------------------------------------------------------------------
        //Работа со Словами в БД
        //--------------------------------------------------------------------------------------------
        public void WordsClear()
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = $"DELETE FROM words; DELETE FROM parents;";
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Возвращает слово по идентификатору i
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Word GetWord(int i)
        {
            if (db == null || db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = $"SELECT id,rank,symbol,childs FROM words WHERE id={i} LIMIT 1;";
            SQLiteDataReader reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;
            int rank = reader.GetInt32(1);
            string symbol = reader.GetString(2);
            int[] childs = StringToIntArray(reader.GetString(3));
            return new Word(i, rank, symbol, childs, null /*parents*/);
        }

        /// <summary>
        /// Вовзращает набор список слов по набору идентификаторов
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public List<Word> GetWords(IEnumerable<int> ids)
        {
            if (db == null || db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            string ids_str = ids.Distinct().Aggregate("", (c, n) => c + (c == "" ? "" : ",") + n.ToString());
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = $"SELECT id,rank,symbol,childs FROM words WHERE id IN ({ids_str});";
            SQLiteDataReader reader = cmd.ExecuteReader();
            List<Word> result = new List<Word>();
            while (reader.Read())
            {
                int i = int.Parse(reader.GetString(0));
                int rank = int.Parse(reader.GetString(1));
                string symbol = reader.GetString(2);
                int[] childs = StringToIntArray(reader.GetString(3));
                result.Add(new Word(i, rank, symbol, childs, null /*parents*/));
            }
            return result;
        }

        /// <summary>
        /// Возвращает Слово ранга 0 из хранилища по тексту s
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public int GetWordId(string s)
        {
            if (alphabet.TryGetValue(s, out int id)) return id;
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = $"SELECT words.id FROM words WHERE symbol='{s}' LIMIT 1;";
            object result = cmd.ExecuteScalar();
            if (result == null) return 0;
            if (int.TryParse(result.ToString(), out id))
            {
                alphabet[s] = id;
                return id;
            }
            else return 0;
        }

        /// <summary>
        /// Возвращает слово по тексту. Текст хранится только для слов ранга 0
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public Word GetWord(string s)
        {
            if (alphabet.TryGetValue(s, out int id)) return new Word(id, 0, s, null, null);
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = $"SELECT words.id FROM words WHERE symbol='{s}' LIMIT 1;";
            object result = cmd.ExecuteScalar();
            if (result == null) return null;
            if (int.TryParse(result.ToString(), out id))
            {
                alphabet[s] = id;
                return new Word(id, 0, s, null, null);
            }
            else return null;
        }

        /// <summary>
        /// Возвращает id слова по дочерним _childs
        /// </summary>
        /// <param name="_childs"></param>
        /// <returns></returns>
        public int GetWordIdByChilds(int[] _childs)
        {
            Sequence childs = new Sequence(_childs);
            if (words_id.TryGetValue(childs, out int id)) return id;
            string childs_str = _childs.Aggregate("", (c, n) => c + (c == "" ? "" : ",") + n.ToString());
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = $"SELECT words.id FROM words WHERE childs='{childs_str}' LIMIT 1;";
            object result = cmd.ExecuteScalar();
            if (result == null) return 0;
            //запишем в кэш и вернем
            if (int.TryParse(result.ToString(), out id))
            {
                words_id[childs] = id;
                return id;
            }
            else return 0;
        }

        /// <summary>
        /// Возвращает Слово у которого дочерние слова - _childs
        /// </summary>
        /// <param name="_childs"></param>
        /// <returns></returns>
        public Word GetWordByChilds(int[] _childs)
        {
            if (db == null || db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            if (_childs == null || _childs.Length == 0) return null;
            string childs_str = IntArrayToString(_childs);
            string[] word = SelectValues(tablename: "words", columns: "id,rank,symbol,childs", where: $"childs='{childs_str}'").FirstOrDefault();
            if (word == null) return null;
            int id = int.Parse(word[0]);
            int rank = int.Parse(word[1]);
            string symbol = word[2];
            //var parents_qry = SQLiteHelper.SelectValues(db, tablename: "parents", columns: "id,parent_id", where: $"id={id}");
            //int[] parents = parents_qry.Select(s => int.Parse(s[1])).ToArray();
            return new Word(id, rank, symbol, _childs, null/*parents*/);
        }

        /// <summary>
        /// Возвращает родителей Слова с id=i
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public IEnumerable<Word> GetWordParents(int i)
        {
            if (db == null || db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText =
                $"SELECT DISTINCT words.id, words.rank, words.symbol, words.childs FROM words " +
                $"INNER JOIN parents ON words.id = parents.parent_id WHERE parents.id={i} ;";
            //$"WHERE words.id IN (SELECT parents.parent_id FROM parents WHERE parents.id = {i});";
            SQLiteDataReader reader = cmd.ExecuteReader();
            List<Word> words = new List<Word>();
            while (reader.Read())
            {
                int[] childs = StringToIntArray(reader.GetString(3));
                Word w = new Word(int.Parse(reader.GetString(0)), int.Parse(reader.GetString(1)), reader.GetString(2), childs, null);
                words.Add(w);
            }
            return words;
        }

        /// <summary>
        /// Возвращает внуков слова с id=i
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public IEnumerable<int> GetWordGrandchildsId(int i)
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText =
                $"SELECT DISTINCT parents1.id FROM parents parents1 " +
                $"INNER JOIN parents parents2 ON parents1.parent_id=parents2.id " +
                $"WHERE parents2.parent_id={i.ToString()}";
            SQLiteDataReader reader = cmd.ExecuteReader();
            List<int> result = new List<int>();
            while (reader.Read())
                result.Add(int.Parse(reader.GetString(0)));
            return result;
        }

        /// <summary>
        /// Возвращает всех внуков для слов с идентификаторами из набора i
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public IEnumerable<int> GetWordsGrandchildsId(IEnumerable<int> i)
        {
            string i_str = i.Aggregate("", (c, n) => c + (c == "" ? "" : ",") + n.ToString());
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText =
                $"SELECT DISTINCT words.childs FROM words " +
                $"WHERE words.id IN ({i_str}) ;";
            SQLiteDataReader reader = cmd.ExecuteReader();
            StringBuilder childs = new StringBuilder();
            while (reader.Read())
            {
                if (childs.Length > 0) childs.Append(",");
                childs.Append(reader.GetString(0));
            }
            reader.Close();
            cmd.CommandText =
                $"SELECT DISTINCT parents.id FROM parents " +
                $"WHERE parents.parent_id IN ({childs.ToString()})";
            reader = cmd.ExecuteReader();
            List<int> result = new List<int>();
            while (reader.Read())
            {
                int id = int.Parse(reader.GetString(0));
                result.Add(id);
            }
            return result;
        }

        /// <summary>
        /// Возвращает набор пар вида (id родителя, Cлово)
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public IEnumerable<Tuple<int, Word>> GetWordsParentsWithChilds(int[] i)
        {
            if (db == null || db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            string ids = i.Select(e => e.ToString()).Aggregate("", (c, n) => c + (c == "" ? "" : ",") + n.ToString());
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText =
                $"SELECT DISTINCT words.id, words.rank, words.symbol, words.childs, parents.id FROM words " +
                $"INNER JOIN parents ON words.id = parents.parent_id WHERE parents.id IN ({ids})";
            SQLiteDataReader reader = cmd.ExecuteReader();
            List<Tuple<int, Word>> words = new List<Tuple<int, Word>>();
            while (reader.Read())
            {
                int[] childs = StringToIntArray(reader.GetString(3));
                Word w = new Word(int.Parse(reader.GetString(0)), int.Parse(reader.GetString(1)), reader.GetString(2), childs, null);
                words.Add(new Tuple<int, Word>(int.Parse(reader.GetString(4)), w));
            }
            return words;
        }

        /// <summary>
        /// Возвращает родителей для слов
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public IEnumerable<int> GetWordsParentsId(IEnumerable<int> i)
        {
            StringBuilder builder = new StringBuilder();
            foreach (int e in i)
            {
                if (builder.Length > 0) builder.Append(",");
                builder.Append(e.ToString());
            }

            string ids = builder.ToString();
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText =
                $"SELECT DISTINCT parents.parent_id FROM parents " +
                $"WHERE parents.id IN ({ids})";
            SQLiteDataReader reader = cmd.ExecuteReader();
            List<int> words = new List<int>();
            while (reader.Read()) words.Add(int.Parse(reader.GetString(0)));
            return words;
        }

        public IEnumerable<int> GetWordParentsId(int i)
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText =
                $"SELECT DISTINCT parents.parent_id FROM parents " +
                $"WHERE parents.id = {i})";
            SQLiteDataReader reader = cmd.ExecuteReader();
            List<int> words = new List<int>();
            while (reader.Read()) words.Add(int.Parse(reader.GetString(0)));
            return words;
        }


        /// <summary>
        /// Добавляет Слово в хранилище и возвращает сгенерированный id Слова
        /// </summary>
        /// <param name="w"></param>
        /// <returns></returns>
        public int AddWord(Word w)
        {
            w.id = NextId();
            string childs = IntArrayToString(w.childs);
            string parents = BuildWordParentsString(w);
            string word = $"({w.id.ToString()}, {w.rank.ToString()}, '{w.symbol}', '{childs}')";
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText =
                $"INSERT INTO words(id,rank,symbol,childs) VALUES {word};" +
                (parents == "" ? "" : $"INSERT INTO parents(id,parent_id) VALUES {parents};");
            cmd.ExecuteNonQuery();
            return w.id;
        }

        /// <summary>
        /// Для Слова w формирует строку из пар сын-родитель вида: 
        /// ('id Слова 1', 'id родителя'), ('id Слова 2', 'id родителя'),...,('id Слова n', 'id родителя')
        /// </summary>
        /// <param name="w"></param>
        /// <returns></returns>
        private string BuildWordParentsString(Word w)
        {
            if (w.childs == null || w.childs.Length == 0) return "";
            StringBuilder builder = new StringBuilder();
            Array.ForEach(w.childs, c => builder.Append((builder.Length == 0 ? "" : ",") + $"({c},{w.id})"));
            return builder.ToString();
        }

        //-------------------------------------------------------------------------------------------------------------------------
        //Работа с кэшем
        //-------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Инициализирует кэш для Алфавита считывая Слова из хранилища
        /// </summary>
        internal void CreateCash()
        {
            terms.Clear();
            words_id.Clear();
            alphabet.Clear();
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = "SELECT id, rank, symbol FROM words WHERE rank=0;";   //буквы алфавита, т.е. слова ранга 0
            SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Word a = new Word(
                    _id: reader.GetInt32(0),
                    _rank: reader.GetInt32(1),
                    _symbol: reader.GetString(2),
                    _childs: null,
                    _parents: null);
                alphabet[a.symbol] = a.id;
            }
        }

        internal void ClearCash()
        {
            alphabet.Clear();
            terms.Clear();
            words_id.Clear();
            GC.Collect();
        }

        //-------------------------------------------------------------------------------------------------------------------------
        //Запросы к БД с минимум параметров
        //-------------------------------------------------------------------------------------------------------------------------
        private SQLiteDataReader ExecuteQuery(string text)
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = text;
            try { return cmd.ExecuteReader(); }
            catch (SQLiteException e) { throw new SQLiteException($"Ошибка выполнения запроса {text} БД({db.FileName}): {e.Message}"); }
        }

        private object ExecuteScalar(string text)
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = text;
            return cmd.ExecuteScalar();
        }

        private List<string[]> SelectValues(string tablename, string columns = "*", string where = "", string limit = "", string order = "")
        {
            string cmd_text = $"SELECT {columns} FROM {tablename}" +
                 (where == "" ? "" : " WHERE " + where) +
                 (limit == "" ? "" : " LIMIT " + limit) +
                 (order == "" ? "" : " ORDER BY " + order);
            List<string[]> values = new List<string[]>();
            SQLiteDataReader reader = ExecuteQuery(cmd_text);
            while (reader.Read())
            {
                string[] row = new string[columns.Length];
                reader.GetValues(row);
                values.Add(row);
            }
            return values;
        }

        private string IntArrayToString(int[] a)
        {
            if (a == null || a.Length == 0) return "";
            StringBuilder builder = new StringBuilder();
            Array.ForEach(a, e => builder.Append((builder.Length == 0 ? "" : ",") + e.ToString()));
            return builder.ToString();
        }

        private int[] StringToIntArray(string s)
        {
            return s.Split(separator: new char[] { ',' }, options: StringSplitOptions.RemoveEmptyEntries).Select(e => int.Parse(e)).ToArray();
        }

        /// <summary>
        /// Генерирует очередной неиспользованный Id. Алгоритм линейный с приращением 1.
        /// TODO: рассмотреть возможности и преимущества генерации id при помощи РСЛОС, лин. конгруэнтных методов, 
        /// членов мультипликативной группы примитивного корня простого числа.
        /// </summary>
        /// <returns></returns>
        private int NextId()
        {
            Debug.WriteLineIf(current_id % (1 << 16) == 0, current_id);
            current_id++;
            return current_id;
        }

        /// <summary>
        /// Возвращает текущий (максимальный) занятый Id хранилища
        /// </summary>
        private int CurrentId
        {
            get
            {
                //т.к. id слова не может быть равен 0, current_id==0 говорит о том, что он не инициализирован
                if (current_id != 0) return current_id;
                if (db == null || db.State != System.Data.ConnectionState.Open)
                    throw new Exception($"Подключение к БД не установлено");
                current_id = Max("words", "id");
                return current_id;
            }
        }

        private Word StringsToWord(string s_id, string s_rank, string s_symbol, string s_childs, IEnumerable<string> s_parents)
        {
            int id = int.Parse(s_id);
            int rank = int.Parse(s_rank);
            string symbol = s_symbol;
            int[] childs = StringToIntArray(s_childs);
            int[] parents = s_parents?.Select(p => int.Parse(p)).ToArray();
            return new Word(id, rank, symbol, childs, parents);
        }

        public IList<int> GetWordsId(int rank)
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = $"SELECT id FROM words WHERE rank={rank}";
            List<int> result = new List<int>();
            SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(reader.GetInt32(0));
            }
            return result;
        }

        // Методы IEnumerable
        public IEnumerator<Word> GetEnumerator()
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = $"SELECT id,rank,symbol,childs FROM words";
            SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Word w = new Word(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), StringToIntArray(reader.GetString(3)), null);
                yield return w;
                //return StringsToWord(s_id: row[0], s_rank: row[1], s_symbol: row[2], s_childs: row[3], s_parents: null/*parents*/);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            db.Close();
        }


        //public void AddRulesSeq(int[] ids)
        //{
        //    int id = 0;
        //    SQLiteCommand cmd = this.db.CreateCommand();
        //    for (int pos = 0; pos < ids.Length; pos++)
        //    {
        //        cmd.CommandText = $"SELECT count FROM grammar WHERE id={id} and next = {ids[pos]} and pos={pos}";
        //        object result = cmd.ExecuteScalar();
        //        int count = result == null ? 0 : (int)result;
        //        if (count > 0)
        //            cmd.CommandText = $"UPDATE grammar SET count={count + 1} WHERE id={id} and next = {ids[pos]} and pos={pos}";
        //        else
        //            cmd.CommandText = $"INSERT INTO grammar(id,next,pos,count) VALUES ({id},{ids[pos]},{pos},{count + 1});";
        //        id = ids[pos];  // запоминаем предыдыущий id
        //        cmd.ExecuteNonQuery();
        //    }
        //}

        //public List<Rule> GetNextRules(Rule rule)
        //{
        //    SQLiteCommand cmd = this.db.CreateCommand();
        //    cmd.CommandText = $"SELECT next, pos, count FROM grammar WHERE id={rule.id} and pos={rule.pos}";
        //    SQLiteDataReader reader = cmd.ExecuteReader();
        //    List<Rule> rules = new List<Rule>();
        //    while (reader.Read())
        //    {
        //        Rule child = new Rule(reader.GetInt32(0), reader.GetInt32(2));
        //        rules.Add(child);
        //    }
        //    return rules;
        //}

        //public Rule GetRule(int id, int pos)
        //{
        //    SQLiteCommand cmd = this.db.CreateCommand();
        //    cmd.CommandText = $"SELECT next, pos, count FROM grammar WHERE id={id} and pos={pos}";
        //    SQLiteDataReader reader = cmd.ExecuteReader();
        //    Rule rule = new Rule(id, 1, pos - 1);
        //    while (reader.Read())
        //    {
        //        Rule child = new Rule(reader.GetInt32(0), reader.GetInt32(2));
        //        rule.Rules.Add(child);
        //    }
        //    return rule;
        //}
    }

}
