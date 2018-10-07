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
        private readonly Dictionary<int, Term> terms = new Dictionary<int, Term>(1 << 18);

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
                "CREATE TABLE splitters (rank, expr);"
                + "CREATE TABLE words (id PRIMARY KEY, rank, symbol, childs);"
                + "CREATE TABLE parents (id, parent_id);";
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
                + "CREATE INDEX words_id_ind ON words (id);";
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
        public Term ToTerm(Word w, float confidence = 1)
        {
            //if (this.terms.TryGetValue(w.id, out Term t)) return t;
            if (w == null) return null;
            Term t = new Term(
                w.rank,
                w.id,
                _confidence: confidence,
                _text: w.symbol,
                _childs: w.rank == 0 ? null : w.childs.Select(c => ToTerm(c)));
            //Сохраняем в кэш
            terms[w.id] = t;
            return t;
        }

        public Term ToTerm(int i, float confidence = 1)
        {
            if (terms.TryGetValue(i, out Term t))
                t.confidence = confidence;
            else
                t = ToTerm(Get(i));
            return t;
        }

        public IEnumerable<Term> ToTerms(IEnumerable<int> ids)
        {
            return Get(ids).Select(w => ToTerm(w));
        }

        internal void ReadSplitters()
        {
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = $"SELECT rank, expr FROM splitters;";
            SQLiteDataReader reader = cmd.ExecuteReader();
            List<Tuple<int, string>> db_splitters = new List<Tuple<int, string>>();
            while (reader.Read())
            {
                int rank = int.Parse(reader.GetString(0));
                string expr = reader.GetString(1);
                db_splitters.Add(new Tuple<int, string>(rank, expr));
            }
            Splitters = db_splitters.OrderBy(t => t.Item1).Select(t => t.Item2).ToArray();
        }

        //--------------------------------------------------------------------------------------------
        //Работа со Словами в БД
        //--------------------------------------------------------------------------------------------
        /// <summary>
        /// Возвращает слово по идентификатору i
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Word Get(int i)
        {
            if (db == null || db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText = $"SELECT id,rank,symbol,childs FROM words WHERE id='{i}' LIMIT 1;";
            SQLiteDataReader reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;
            int rank = int.Parse(reader.GetString(1));
            string symbol = reader.GetString(2);
            int[] childs = StringToIntArray(reader.GetString(3));
            return new Word(i, rank, symbol, childs, null /*parents*/);
        }

        /// <summary>
        /// Вовзращает набор список слов по набору идентификаторов
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public List<Word> Get(IEnumerable<int> ids)
        {
            if (db == null || db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            string ids_str = ids.Distinct().Aggregate("", (c, n) => c + (c == "" ? "" : ",") + "'" + n.ToString() + "'");
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
        public int GetId(string s)
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
        public Word Get(string s)
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
        public int GetIdByChilds(int[] _childs)
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
        public Word GetByChilds(int[] _childs)
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
            //var parents_qry = SQLiteHelper.SelectValues(db, tablename: "parents", columns: "id,parent_id", where: $"id='{id}'");
            //int[] parents = parents_qry.Select(s => int.Parse(s[1])).ToArray();
            return new Word(id, rank, symbol, _childs, null/*parents*/);
        }

        /// <summary>
        /// Возвращает родителей Слова с id=i
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public IEnumerable<Word> GetParents(int i)
        {
            if (db == null || db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            SQLiteCommand cmd = db.CreateCommand();
            cmd.CommandText =
                $"SELECT DISTINCT words.id, words.rank, words.symbol, words.childs FROM words " +
                $"INNER JOIN parents ON words.id = parents.parent_id WHERE parents.id='{i}' ;";
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
        public IEnumerable<int> GetGrandchildsId(int i)
        {
            SQLiteCommand cmd = db.CreateCommand();
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

        /// <summary>
        /// Возвращает всех внуков для слов с идентификаторами из набора i
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public IEnumerable<int> GetGrandchildsId(IEnumerable<int> i)
        {
            string i_str = i.Aggregate("", (c, n) => c + (c == "" ? "" : ",") + "'" + n.ToString() + "'");
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
        public IEnumerable<Tuple<int, Word>> GetParentsWithChilds(int[] i)
        {
            if (db == null || db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            string ids = i.Select(e => "'" + e.ToString() + "'").Aggregate("", (c, n) => c + (c == "" ? "" : ",") + n.ToString());
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
        public IEnumerable<int> GetParentsId(IEnumerable<int> i)
        {
            StringBuilder builder = new StringBuilder();
            foreach (int e in i)
            {
                if (builder.Length > 0) builder.Append(",");
                builder.Append("'" + e.ToString() + "'");
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

        /// <summary>
        /// Добавляет Слово в хранилище и возвращает сгенерированный id Слова
        /// </summary>
        /// <param name="w"></param>
        /// <returns></returns>
        public int Add(Word w)
        {
            w.id = NextId();
            string childs = IntArrayToString(w.childs);
            string parents = BuildParentsString(w);
            string word = $"('{w.id.ToString()}', '{w.rank.ToString()}', '{w.symbol}', '{childs}')";
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
        private string BuildParentsString(Word w)
        {
            if (w.childs == null || w.childs.Length == 0) return "";
            StringBuilder builder = new StringBuilder();
            Array.ForEach(w.childs, c => builder.Append((builder.Length == 0 ? "" : ",") + $"('{c}','{w.id}')"));
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
                    _id: int.Parse(reader.GetString(0)),
                    _rank: int.Parse(reader.GetString(1)),
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

        // Методы IEnumerable
        public IEnumerator<Word> GetEnumerator()
        {
            SQLiteCommand cmd = db.CreateCommand();
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
                yield return StringsToWord(s_id: row[0], s_rank: row[1], s_symbol: row[2], s_childs: row[3], s_parents: null/*parents*/);
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
