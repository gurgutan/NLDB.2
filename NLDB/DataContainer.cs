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
        private SQLiteTransaction transaction;
        private SQLiteConnection db;

        //Кэш термов для быстрого выполнения метода ToTerm
        private readonly Dictionary<int, Term> terms = new Dictionary<int, Term>(1 << 10);

        //Кэш символов алфавита
        private readonly Dictionary<string, int> alphabet = new Dictionary<string, int>(1 << 10);

        //Кэш id слов для поиска по childs
        private readonly Dictionary<Sequence, int> words_id = new Dictionary<Sequence, int>(1 << 10);

        private int current_id = 0;

        private string[] splitters;
        public string[] Splitters
        {
            get => this.splitters;
            set => this.splitters = value;
        }

        private string dbname = "data.db";

        public DataContainer(string _dbname, string[] _splitters)
        {
            this.dbname = _dbname;
            this.splitters = _splitters;
            this.current_id = 0;
            //this.Create();
            //db = SQLiteHelper.OpenConnection(dbname);
        }

        /// <summary>
        /// Возвращает количество слов в базе данных
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return int.Parse(ExecuteScalar($"SELECT COUNT(*) FROM words;").ToString());
        }

        //--------------------------------------------------------------------------------------------
        //Работа с базой данных SQLite
        //--------------------------------------------------------------------------------------------
        public bool IsOpen()
        {
            if (this.db == null) return false;
            return this.db.State == System.Data.ConnectionState.Open;
        }

        public void CreateDB()
        {
            this.current_id = 0;
            if (this.IsOpen()) this.CloseConnection();                        // Закрываем
            if (File.Exists(this.dbname)) File.Delete(this.dbname); // Удаляем БД
            this.db = new SQLiteConnection($"Data Source={this.dbname}; Version=3;");  // Создаем новую БД
            this.db.Open();     // Открываем соединение
            //Создаем таблицы
            SQLiteCommand cmd = this.db.CreateCommand();
            cmd.CommandText =
                "CREATE TABLE splitters (rank, expr);"
                + "CREATE TABLE words (id PRIMARY KEY, rank, symbol, childs);"
                + "CREATE TABLE parents (id, parent_id);";
            //+"CREATE TABLE grammar (id, next INTEGER NOT NULL, pos INTEGER NOT NULL, count INTEGER NOT NULL );";
            cmd.ExecuteNonQuery();
            //Добавляем разделители слов в таблицу splitters
            string splitters_values = this.splitters.
                Select((s, i) => $"({i},'{s}')").
                Aggregate("", (c, n) => c + (c == "" ? "" : ",") + n);
            cmd.CommandText = $"INSERT INTO splitters (rank, expr) VALUES {splitters_values}";
            cmd.ExecuteNonQuery();
            //Создаем индексы
            cmd.CommandText =
                "CREATE INDEX childs_ind ON words (childs);"
                + "CREATE INDEX parents_id_ind ON parents (id);"
                + "CREATE INDEX parents_p_id_ind ON parents (parent_id);"
                + "CREATE INDEX words_id_ind ON words (id);";
            //+"CREATE INDEX grammar_id_ind ON grammar (id);" 
            //+"CREATE INDEX grammar_id_next_pos_ind ON grammar (id, next ASC, pos ASC);";
            cmd.ExecuteNonQuery();
            this.db.Close();
        }

        public SQLiteConnection Connect(string _dbname)
        {
            this.dbname = _dbname;
            //if (db.State == System.Data.ConnectionState.Open) return db;
            //TODO: переделать для безопасного использования (try catch и т.п.)
            this.db = new SQLiteConnection($"Data Source={this.dbname}; Version=3;");  // Создаем новую БД
            this.db.Open();
            this.current_id = 0;
            this.current_id = this.CurrentId;
            this.CreateCash();
            return this.db;
        }

        public void CloseConnection()
        {
            if (this.db.State == System.Data.ConnectionState.Open)
                this.db.Close();
        }

        public List<string[]> SelectValues(string tablename, string columns = "*", string where = "", string limit = "", string order = "")
        {
            string cmd_text = $"SELECT {columns} FROM {tablename}" +
                 (where == "" ? "" : " WHERE " + where) +
                 (limit == "" ? "" : " LIMIT " + limit) +
                 (order == "" ? "" : " ORDER BY " + order);
            List<string[]> values = new List<string[]>();
            SQLiteDataReader reader = this.ExecuteQuery(cmd_text);
            while (reader.Read())
            {
                string[] row = new string[columns.Length];
                reader.GetValues(row);
                values.Add(row);
            }
            return values;
        }

        public int Max(string tablename, string column)
        {
            string str = ExecuteScalar($"SELECT MAX(cast({column} as INTEGER)) FROM {tablename};").ToString();
            return str == "" ? 0 : int.Parse(str);
        }

        private SQLiteDataReader ExecuteQuery(string text)
        {
            SQLiteCommand cmd = this.db.CreateCommand();
            cmd.CommandText = text;
            try { return cmd.ExecuteReader(); }
            catch (SQLiteException e) { throw new SQLiteException($"Ошибка выполнения запроса {text} БД({this.db.FileName}): {e.Message}"); }
        }

        private object ExecuteScalar(string text)
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = text;
            return cmd.ExecuteScalar();
        }

        public void BeginTransaction()
        {
            this.transaction = this.db.BeginTransaction();
        }

        public void EndTransaction()
        {
            this.transaction.Commit();
        }

        //--------------------------------------------------------------------------------------------
        //Преобразование Слова в Терм
        //--------------------------------------------------------------------------------------------
        public Term ToTerm(Word w, float confidence = 1)
        {
            if (this.terms.TryGetValue(w.id, out Term t)) return t;
            t = new Term(
                w.rank,
                w.id,
                _confidence: confidence,
                _text: w.symbol,
                _childs: w.rank == 0 ? null : w.childs.Select(c => this.ToTerm(c)));
            this.terms[w.id] = t;
            return t;
        }

        public Term ToTerm(int i, float confidence = 1)
        {
            if (this.terms.TryGetValue(i, out Term t))
                t.confidence = confidence;
            else
                t = this.ToTerm(this.Get(i));
            return t;
        }

        //Создание кэша для алфавита
        internal void CreateCash()
        {
            this.alphabet.Clear();
            SQLiteCommand cmd = this.db.CreateCommand();
            cmd.CommandText = "SELECT id, rank, symbol FROM words WHERE rank=0;";   //буквы алфавита, т.е. слова ранга 0
            SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Word a = new Word(
                    _id: int.Parse(reader.GetString(0)),
                    _rank: int.Parse(reader.GetString(1)),
                    _symbol: reader.GetString(2),
                    _childs: null,
                    _parents: null);
                this.alphabet[a.symbol] = a.id;
            }
        }

        internal void ClearCash()
        {
            this.alphabet.Clear();
            this.terms.Clear();
            this.words_id.Clear();
            GC.Collect();
        }

        public Word Get(int i)
        {
            if (this.db == null || this.db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            SQLiteCommand cmd = this.db.CreateCommand();
            cmd.CommandText = $"SELECT id,rank,symbol,childs FROM words WHERE id='{i}' LIMIT 1;";
            SQLiteDataReader reader = cmd.ExecuteReader();
            reader.Read();
            //var word = SQLiteHelper.SelectValues(db, tablename: "words", columns: "id,rank,symbol,childs", where: $"id='{i}'", limit: "").FirstOrDefault();
            //if (word == null) return null;
            int rank = int.Parse(reader.GetString(1));
            string symbol = reader.GetString(2);
            int[] childs = this.StringToIntArray(reader.GetString(3));
            //var parents_qry = SQLiteHelper.SelectValues(db,
            //    tablename: "parents",
            //    columns: "id,parent_id",
            //    where: $"id='{i}'");
            //int[] parents = parents_qry.Select(s => int.Parse(s[1])).ToArray();
            return new Word(i, rank, symbol, childs, null /*parents*/);
        }

        public int GetId(string s)
        {
            if (this.alphabet.TryGetValue(s, out int id)) return id;
            SQLiteCommand cmd = this.db.CreateCommand();
            cmd.CommandText = $"SELECT words.id FROM words WHERE symbol='{s}' LIMIT 1;";
            object result = cmd.ExecuteScalar();
            if (result == null) return 0;
            if (int.TryParse(result.ToString(), out id))
            {
                this.alphabet[s] = id;
                return id;
            }
            else return 0;
        }

        public Word Get(string s)
        {
            if (this.alphabet.TryGetValue(s, out int id)) return new Word(id, 0, s, null, null);
            SQLiteCommand cmd = this.db.CreateCommand();
            cmd.CommandText = $"SELECT words.id FROM words WHERE symbol='{s}' LIMIT 1;";
            object result = cmd.ExecuteScalar();
            if (result == null) return null;
            if (int.TryParse(result.ToString(), out id))
            {
                this.alphabet[s] = id;
                return new Word(id, 0, s, null, null);
            }
            else return null;
        }

        public int GetId(int[] _childs)
        {
            Sequence childs = new Sequence(_childs);
            if (this.words_id.TryGetValue(childs, out int id)) return id;
            string childs_str = _childs.Aggregate("", (c, n) => c + (c == "" ? "" : ",") + n.ToString());
            SQLiteCommand cmd = this.db.CreateCommand();
            cmd.CommandText = $"SELECT words.id FROM words WHERE childs='{childs_str}' LIMIT 1;";
            object result = cmd.ExecuteScalar();
            if (result == null) return 0;
            //запишем в кэш
            if (int.TryParse(result.ToString(), out id)) return id;
            else return 0;
        }

        public Word Get(int[] _childs)
        {
            if (this.db == null || this.db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            if (_childs == null || _childs.Length == 0) return null;
            string childs_str = this.IntArrayToString(_childs);
            string[] word = SelectValues(tablename: "words", columns: "id,rank,symbol,childs", where: $"childs='{childs_str}'").FirstOrDefault();
            if (word == null) return null;
            int id = int.Parse(word[0]);
            int rank = int.Parse(word[1]);
            string symbol = word[2];
            //var parents_qry = SQLiteHelper.SelectValues(db, tablename: "parents", columns: "id,parent_id", where: $"id='{id}'");
            //int[] parents = parents_qry.Select(s => int.Parse(s[1])).ToArray();
            return new Word(id, rank, symbol, _childs, null/*parents*/);
        }

        public IEnumerable<Word> GetParents(int i)
        {
            if (this.db == null || this.db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            SQLiteCommand cmd = this.db.CreateCommand();
            cmd.CommandText =
                $"SELECT DISTINCT words.id, words.rank, words.symbol, words.childs FROM words " +
                $"INNER JOIN parents ON words.id = parents.parent_id WHERE parents.id='{i}' ;";
            //$"WHERE words.id IN (SELECT parents.parent_id FROM parents WHERE parents.id = {i});";
            SQLiteDataReader reader = cmd.ExecuteReader();
            List<Word> words = new List<Word>();
            while (reader.Read())
            {
                int[] childs = this.StringToIntArray(reader.GetString(3));
                Word w = new Word(int.Parse(reader.GetString(0)), int.Parse(reader.GetString(1)), reader.GetString(2), childs, null);
                words.Add(w);
            }
            return words;
        }

        public IEnumerable<int> GetGrandchildsId(int i)
        {
            SQLiteCommand cmd = this.db.CreateCommand();
            cmd.CommandText =
                $"SELECT DISTINCT parents1.id FROM parents parents1 " +
                $"INNER JOIN parents parents2 ON parents1.parent_id=parents2.id " +
                $"WHERE parents2.parent_id='{i.ToString()}'";
            SQLiteDataReader reader = cmd.ExecuteReader();
            List<int> result = new List<int>();
            while (reader.Read())
                result.Add(int.Parse(reader.GetString(0)));
            return result;
        }

        public IEnumerable<int> GetGrandchildsId(IEnumerable<int> i)
        {
            string i_str = i.Aggregate("", (c, n) => c + (c == "" ? "" : ",") + "'" + n.ToString() + "'");
            SQLiteCommand cmd = this.db.CreateCommand();
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

        public IEnumerable<Tuple<int, Word>> GetParentsWithChilds(int[] i)
        {
            if (this.db == null || this.db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            string ids = i.Select(e => "'" + e.ToString() + "'").Aggregate("", (c, n) => c + (c == "" ? "" : ",") + n.ToString());
            SQLiteCommand cmd = this.db.CreateCommand();
            cmd.CommandText =
                $"SELECT DISTINCT words.id, words.rank, words.symbol, words.childs, parents.id FROM words " +
                $"INNER JOIN parents ON words.id = parents.parent_id WHERE parents.id IN ({ids})";
            SQLiteDataReader reader = cmd.ExecuteReader();
            List<Tuple<int, Word>> words = new List<Tuple<int, Word>>();
            while (reader.Read())
            {
                int[] childs = this.StringToIntArray(reader.GetString(3));
                Word w = new Word(int.Parse(reader.GetString(0)), int.Parse(reader.GetString(1)), reader.GetString(2), childs, null);
                words.Add(new Tuple<int, Word>(int.Parse(reader.GetString(4)), w));
            }
            return words;
        }

        public IEnumerable<int> GetParentsId(int[] i)
        {
            StringBuilder builder = new StringBuilder();
            Array.ForEach(i, e => { if (builder.Length > 0) builder.Append(","); builder.Append("'" + e.ToString() + "'"); });
            string ids = builder.ToString();
            SQLiteCommand cmd = this.db.CreateCommand();
            cmd.CommandText =
                $"SELECT DISTINCT parents.parent_id FROM parents " +
                $"WHERE parents.id IN ({ids})";
            SQLiteDataReader reader = cmd.ExecuteReader();
            List<int> words = new List<int>();
            while (reader.Read()) words.Add(int.Parse(reader.GetString(0)));
            return words;
        }

        public int Add(Word w)
        {
            w.id = this.NextId();
            string childs = this.IntArrayToString(w.childs);
            string parents = this.BuildParentsString(w);
            string word = $"('{w.id.ToString()}', '{w.rank.ToString()}', '{w.symbol}', '{childs}')";
            SQLiteCommand cmd = this.db.CreateCommand();
            cmd.CommandText =
                $"INSERT INTO words(id,rank,symbol,childs) VALUES {word};" +
                (parents == "" ? "" : $"INSERT INTO parents(id,parent_id) VALUES {parents};");
            cmd.ExecuteNonQuery();
            return w.id;
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

        private string BuildParentsString(Word w)
        {
            if (w.childs == null || w.childs.Length == 0) return "";
            StringBuilder builder = new StringBuilder();
            Array.ForEach(w.childs, c => builder.Append((builder.Length == 0 ? "" : ",") + $"('{c}','{w.id}')"));
            return builder.ToString();
        }

        //-------------------------------------------------------------------------------------------------------------------------
        //Private methods
        //-------------------------------------------------------------------------------------------------------------------------
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

        private int NextId()
        {
            Debug.WriteLineIf(this.current_id % (1 << 16) == 0, this.current_id);
            this.current_id++;
            return this.current_id;
        }

        private int CurrentId
        {
            get
            {
                //т.к. id слова не может быть равен 0, current_id==0 говорит о том, что он не инициализирован
                if (this.current_id != 0) return this.current_id;
                if (this.db == null || this.db.State != System.Data.ConnectionState.Open)
                    throw new Exception($"Подключение к БД не установлено");
                this.current_id = Max("words", "id");
                return this.current_id;
            }
        }

        private Word StringsToWord(string s_id, string s_rank, string s_symbol, string s_childs, IEnumerable<string> s_parents)
        {
            int id = int.Parse(s_id);
            int rank = int.Parse(s_rank);
            string symbol = s_symbol;
            int[] childs = this.StringToIntArray(s_childs);
            int[] parents = s_parents?.Select(p => int.Parse(p)).ToArray();
            return new Word(id, rank, symbol, childs, parents);
        }

        // Методы IEnumerable
        public IEnumerator<Word> GetEnumerator()
        {
            SQLiteCommand cmd = this.db.CreateCommand();
            cmd.CommandText = $"SELECT id,rank,symbol,childs FROM words";
            SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string[] row = new string[4];
                reader.GetValues(row);
                //var parents_qry = SQLiteHelper.SelectValues(db,
                //    tablename: "parents",
                //    columns: "parent_id",
                //    where: $"parents.id={row[0]}");
                //IEnumerable<string> parents = parents_qry.Select(p => p[0]);
                yield return this.StringsToWord(s_id: row[0], s_rank: row[1], s_symbol: row[2], s_childs: row[3], s_parents: null/*parents*/);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Dispose()
        {
            this.db.Close();
        }
    }

}
