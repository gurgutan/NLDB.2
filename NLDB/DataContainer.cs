using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public class DataContainer : IEnumerable<Word>, IDisposable
    {
        //private int block_size = 1 << 19;

        private SQLiteTransaction transaction;
        private SQLiteConnection db;

        //Кэш термов для быстрого выполнения метода ToTerm
        private Dictionary<int, Term> terms = new Dictionary<int, Term>();

        //Кэш для символов алфавита
        //private Dictionary<string, Word> alphabet = new Dictionary<string, Word>();

        private Dictionary<string, int> alphabet = new Dictionary<string, int>();

        //Кэш id слов для поиска по childs
        Dictionary<Sequence, int> words_id = new Dictionary<Sequence, int>(1 << 24);


        private int current_id = 0;

        private string[] splitters;
        public string[] Splitters
        {
            get { return splitters; }
            set { splitters = value; }
        }

        //private Alphabet alphabet = new Alphabet();
        //private Dictionary<int, Word> i2w = new Dictionary<int, Word>();
        //private Dictionary<Word, int> w2i = new Dictionary<Word, int>();
        //private Dictionary<Sequence, Link[]> links = new Dictionary<Sequence, Link[]>();

        private string dbname = "data.db";

        public DataContainer(string _dbname, string[] _splitters)
        {
            dbname = _dbname;
            splitters = _splitters;
            current_id = 0;
            if (!File.Exists(dbname)) Create();
            //db = SQLiteHelper.OpenConnection(dbname);
        }

        public bool IsOpen()
        {
            if (db == null) return false;
            return db.State == System.Data.ConnectionState.Open;
        }

        public void Create()
        {
            current_id = 0;
            if (File.Exists(dbname)) File.Delete(dbname);
            //if (db.State != System.Data.ConnectionState.Open)
            //    db = SQLiteHelper.OpenConnection(dbname);
            CreateSplittersTable();
            CreateWordsTable();
            CreateLinksTable();
        }

        public Term ToTerm(Word w, float confidence = 1)
        {
            Term t;
            if (terms.TryGetValue(w.id, out t)) return t;
            t = new Term(
                w.rank,
                w.id,
                _confidence: confidence,
                _text: w.symbol,
                _childs: w.rank == 0 ? null : w.childs.Select(c => ToTerm(Get(c))));
            terms[w.id] = t;
            return t;
        }

        public Term ToTerm(int i, float confidence = 1)
        {
            if (terms.ContainsKey(i))
            {
                terms[i].confidence = confidence;
                return terms[i];
            }
            var word = Get(i);
            return ToTerm(word);
        }

        public void BeginTransaction()
        {
            transaction = db.BeginTransaction();
        }

        public void EndTransaction()
        {
            transaction.Commit();
        }

        //public DataContainer(
        //    string[] _splitters,
        //    //Alphabet _alphabet,
        //    Dictionary<int, Word> _i2w,
        //    Dictionary<Word, int> _w2i,
        //    Dictionary<Sequence, Link[]> _links)
        //{
        //    splitters = _splitters;
        //    //alphabet = _alphabet;
        //    i2w = _i2w;
        //    w2i = _w2i;
        //    links = _links;
        //}

        public int Count()
        {
            return SQLiteHelper.Count(db, "words");
        }

        public SQLiteConnection Open(string _dbname)
        {
            dbname = _dbname;
            //if (db.State == System.Data.ConnectionState.Open) return db;
            db = SQLiteHelper.OpenConnection(dbname);
            current_id = 0;
            current_id = this.CurrentId;
            CreateCashes();
            return db;
        }

        //Создание кэша для алфавита
        private void CreateCashes()
        {
            alphabet.Clear();
            var cmd = db.CreateCommand();
            cmd.CommandText = "SELECT id, rank, symbol FROM words WHERE rank=0;";
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

        public void Close() => SQLiteHelper.CloseConnection(db);

        //public int GetSymbol(string s)
        //{
        //    if (db == null || db.State != System.Data.ConnectionState.Open)
        //        throw new Exception($"Подключение к БД не установлено");
        //    var letter = SQLiteHelper.SelectScalar(db,
        //        tablename: "alphabet",
        //        columns: "code",
        //        where: $"letter={s}");
        //    return (letter == null) ? 0 : (int)letter;
        //}

        //public string GetSymbol(int i)
        //{
        //    if (db == null || db.State != System.Data.ConnectionState.Open)
        //        throw new Exception($"Подключение к БД не установлено");
        //    var code = SQLiteHelper.SelectScalar(db,
        //        tablename: "alphabet",
        //        columns: "code",
        //        where: $"code={i}");
        //    return (code == null) ? "" : (string)code;
        //}

        //public bool ContainsSymbol(string s) => GetSymbol(s) != 0;

        //public bool ContainsSymbol(int i) => GetSymbol(i) != null;

        public Word Get(int i)
        {
            if (db == null || db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            var word = SQLiteHelper.SelectValues(db, tablename: "words", columns: "id,rank,symbol,childs", where: $"id='{i}'", limit: "").FirstOrDefault();
            if (word == null) return null;
            var rank = int.Parse(word[1]);
            string symbol = word[2];
            int[] childs = StringToIntArray(word[3]);
            //var parents_qry = SQLiteHelper.SelectValues(db,
            //    tablename: "parents",
            //    columns: "id,parent_id",
            //    where: $"id='{i}'");
            //int[] parents = parents_qry.Select(s => int.Parse(s[1])).ToArray();
            return new Word(i, rank, symbol, childs, null /*parents*/);
        }

        //public Word Get(string s)
        //{
        //    Word w;
        //    //Попытка найти слово в кэше
        //    if (alphabet.TryGetValue(s, out w)) return w;
        //    if (db == null || db.State != System.Data.ConnectionState.Open)
        //        throw new Exception($"Подключение к БД не установлено");
        //    var word = SQLiteHelper.SelectValues(db,
        //        tablename: "words",
        //        columns: "id,rank,symbol,childs",
        //        where: $"symbol='{s}'",
        //        limit: "1").FirstOrDefault();
        //    if (word == null) return null;
        //    int id = int.Parse(word[0]);
        //    var rank = int.Parse(word[1]);
        //    string symbol = word[2];
        //    int[] childs = StringToIntArray(word[3]);
        //    //var parents_qry = SQLiteHelper.SelectValues(db, tablename: "parents", columns: "id,parent_id", where: $"id='{id}'");
        //    //int[] parents = parents_qry.Select(p => int.Parse(p[1])).ToArray();
        //    w = new Word(id, rank, symbol, childs, null /*parents*/);
        //    //Записываем симво в кэш
        //    alphabet[s] = w.id;
        //    return w;
        //}

        public int GetId(string s)
        {
            int id;
            if (alphabet.TryGetValue(s, out id)) return id;
            var cmd = db.CreateCommand();
            cmd.CommandText = $"SELECT words.id FROM words WHERE symbol='{s}' LIMIT 1;";
            object result = cmd.ExecuteScalar();
            if (result == null) return 0;
            if (int.TryParse(result.ToString(), out id)) return id;
            else return 0;

        }

        public int GetId(int[] _childs)
        {
            Sequence childs = new Sequence(_childs);
            int id;
            if (words_id.TryGetValue(childs, out id)) return id;
            string childs_str = _childs.Aggregate("", (c, n) => c + (c == "" ? "" : ",") + n.ToString());
            var cmd = db.CreateCommand();
            cmd.CommandText = $"SELECT words.id FROM words WHERE childs='{childs_str}' LIMIT 1;";
            object result = cmd.ExecuteScalar();
            if (result == null) return 0;
            //запишем в кэш
            if (int.TryParse(result.ToString(), out id)) return id;
            else return 0;
        }

        public Word Get(int[] _childs)
        {
            if (db == null || db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            if (_childs == null || _childs.Length == 0) return null;
            var childs_str = IntArrayToString(_childs);
            var word = SQLiteHelper.SelectValues(db, tablename: "words", columns: "id,rank,symbol,childs", where: $"childs='{childs_str}'").FirstOrDefault();
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
            if (db == null || db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            var cmd = db.CreateCommand();
            cmd.CommandText =
                $"SELECT DISTINCT words.id, words.rank, words.symbol, words.childs FROM words " +
                $"INNER JOIN parents ON words.id = parents.parent_id WHERE parents.id='{i}' ;";
            //$"WHERE words.id IN (SELECT parents.parent_id FROM parents WHERE parents.id = {i});";
            var reader = cmd.ExecuteReader();
            List<Word> words = new List<Word>();
            while (reader.Read())
            {
                int[] childs = StringToIntArray(reader.GetString(3));
                Word w = new Word(int.Parse(reader.GetString(0)), int.Parse(reader.GetString(1)), reader.GetString(2), childs, null);
                words.Add(w);
            }
            return words;
        }

        public IEnumerable<int> GetGrandchildsId(IEnumerable<int> i)
        {
            string i_str = i.Aggregate("", (c, n) => c + (c == "" ? "" : ",") + n.ToString());
            var cmd = db.CreateCommand();
            cmd.CommandText =
                $"SELECT DISTINCT words.childs FROM words " +
                $"WHERE words.id IN '{i_str}' ;";
            var reader = cmd.ExecuteReader();
            StringBuilder childs = new StringBuilder();
            while (reader.Read())
            {
                if (childs.Length > 0) childs.Append(",");
                childs.Append(reader.GetString(0));
            }
            cmd.CommandText =
                $"SELECT DISTINCT parents.id FROM parents" +
                $"WHERE parents.parent_id IN '{childs.ToString()}'";
            reader = cmd.ExecuteReader();
            List<int> result = new List<int>();
            while (reader.Read())
            {
                int id = int.Parse(reader.GetString(0));
                result.Add(id);
            }
            return result;
        }

        public IEnumerable<Tuple<int, Word>> GetParents(int[] i)
        {
            if (db == null || db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            string ids = i.Select(e => "'" + e.ToString() + "'").Aggregate("", (c, n) => c + (c == "" ? "" : ",") + n.ToString());
            var cmd = db.CreateCommand();
            cmd.CommandText =
                $"SELECT DISTINCT words.id, words.rank, words.symbol, words.childs, parents.id FROM words " +
                $"INNER JOIN parents ON words.id = parents.parent_id WHERE parents.id IN ({ids})";
            //$"WHERE words.id IN (SELECT parents.parent_id FROM parents WHERE parents.id IN ({ids}));";
            var reader = cmd.ExecuteReader();
            List<Tuple<int, Word>> words = new List<Tuple<int, Word>>();
            while (reader.Read())
            {
                int[] childs = StringToIntArray(reader.GetString(3));
                Word w = new Word(int.Parse(reader.GetString(0)), int.Parse(reader.GetString(1)), reader.GetString(2), childs, null);
                words.Add(new Tuple<int, Word>(int.Parse(reader.GetString(4)), w));
            }
            return words;
        }

        public IEnumerable<int> GetParentsId(int[] i)
        {
            StringBuilder builder = new StringBuilder();
            Array.ForEach(i, e => { if (builder.Length == 0) builder.Append(","); builder.Append(i); });
            string ids = builder.ToString();
            var cmd = db.CreateCommand();
            cmd.CommandText =
                $"SELECT DISTINCT words.id FROM words " +
                $"INNER JOIN parents ON words.id = parents.parent_id WHERE parents.id IN ({ids})";
            var reader = cmd.ExecuteReader();
            List<int> words = new List<int>();
            while (reader.Read()) words.Add(int.Parse(reader.GetString(0)));
            return words;
        }

        public int Add(Word w)
        {
            w.id = NextId();
            var childs = (w.childs == null || w.childs.Length == 0) ? "" : IntArrayToString(w.childs);
            var word = new List<string[]>{ new string[4]
            {
                w.id.ToString(),
                w.rank.ToString(),
                w.symbol,
                childs
            }};
            var parents = w.childs.Select(c => new string[] { c.ToString(), w.id.ToString() });
            SQLiteHelper.InsertValues(db, "words", "id,rank,symbol,childs", word);
            SQLiteHelper.InsertValues(db, "parents", "id,parent_id", parents);
            return w.id;
        }

        public IEnumerable<Link> GetLinks(int[] seq)
        {
            var seqstr = IntArrayToString(seq);
            var links = SQLiteHelper.SelectValues(db,
                tablename: "links",
                columns: "id,number",
                where: $"links.seq='{seqstr}'",
                order: $"id ASC, number DESC");
            if (links.Count == 0) return null;
            float sum = links.Select(s => int.Parse(s[1])).Sum();
            return links.Select(s => new Link(int.Parse(s[0]), int.Parse(s[1]), int.Parse(s[1]) / sum));
        }

        public Link GetLink(int[] seq, int id)
        {
            var seqstr = IntArrayToString(seq);
            var all_links = SQLiteHelper.SelectValues(db,
                tablename: "links",
                columns: "id,number",
                where: $"links.seq='{seqstr}'");
            var link = all_links.Where(s => s[0] == id.ToString()).FirstOrDefault();
            if (link == null) return default(Link);
            int number = int.Parse(link[1]);
            float sum = all_links.Select(s => int.Parse(s[1])).Sum();
            return new Link(id, number, number / sum);
        }

        public void InsertLink(int[] seq, int id, int number)
        {
            var seqstr = IntArrayToString(seq);
            SQLiteHelper.InsertValues(db,
                "links",
                "seq, id, number",
                new string[][] { new string[] { seqstr, id.ToString(), number.ToString() } });
        }

        public void ReplaceLink(int[] seq, int id, int number)
        {
            var seqstr = IntArrayToString(seq);
            SQLiteHelper.ReplaceValues(db,
                "links",
                "seq, id, number",
                new string[][] { new string[] { seqstr, id.ToString(), number.ToString() } });
        }


        //--------------------------------------------------------------------------------------------------------------------------------------
        //Private methods
        //--------------------------------------------------------------------------------------------------------------------------------------
        private string IntArrayToString(int[] a) =>
            a.Aggregate("", (c, n) => c + (c == "" ? "" : ",") + n.ToString());

        private int[] StringToIntArray(string s) =>
            s.Split(separator: new char[] { ',' }, options: StringSplitOptions.RemoveEmptyEntries).
            Select(e => int.Parse(e)).
            ToArray();

        private int NextId()
        {
            Debug.WriteLineIf(current_id % (1 << 14) == 0, current_id);
            current_id++;
            return current_id;
        }
        private int CurrentId
        {
            get
            {
                //т.к. id слова не может быть равен 0, current_id==0 говорит о том, что он не инициализирован
                if (current_id != 0) return current_id;
                if (db == null || db.State != System.Data.ConnectionState.Open)
                    throw new Exception($"Подключение к БД не установлено");
                current_id = SQLiteHelper.Max(db, "words", "id");
                return current_id;
            }
        }


        private void CreateSplittersTable()
        {
            string columns = "rank, expr";
            var data = splitters.Select((s, i) => new string[2] { s, i.ToString() });
            SQLiteHelper.CreateTable(dbname, "splitters", columns, true);
            SQLiteHelper.InsertValues(dbname, "splitters", columns, data);
        }

        //private void CreateAlphabetTable()
        //{
        //    string[] columns = new string[] { "code", "letter" };
        //    var data = alphabet.Letters.Select(kvp => new string[2] { kvp.Key.ToString(), kvp.Value });
        //    SQLiteHelper.CreateTable(dbname, "alphabet", columns, true);
        //    SQLiteHelper.InsertValues(dbname, "alphabet", columns, data);
        //    SQLiteHelper.CreateIndex(dbname, "alphabet", "code_ind", new string[] { "code" });
        //    SQLiteHelper.CreateIndex(dbname, "alphabet", "letter_ind", new string[] { "letter" });
        //}

        private void CreateWordsTable()
        {
            string columns_words = "id PRIMARY KEY, rank, symbol, childs";
            string columns_parents = "id, parent_id";
            //Создаем таблицы
            SQLiteHelper.CreateTable(dbname, "words", columns_words, true);
            SQLiteHelper.CreateTable(dbname, "parents", columns_parents, true);
            //Создаем индексы
            SQLiteHelper.CreateIndex(dbname, "words", "words_id_ind", "id");
            SQLiteHelper.CreateIndex(dbname, "words", "childs_ind", "childs");
            SQLiteHelper.CreateIndex(dbname, "parents", "parents_id_ind", "id");
        }

        private void CreateLinksTable()
        {
            string columns_links = "seq,id,number";
            SQLiteHelper.CreateTable(dbname, "links", columns_links, true);
            //for (int i = 0; i < links.Count / block_size + 1; i++)
            //{
            //    //"Вырезаем" block_size элементов из links, начиная с элемента, следующего за ранее обработанным блоком
            //    var links_data = links.
            //        Skip(i * block_size).
            //        Take(block_size).
            //        SelectMany(kvp => kvp.Value.
            //        Select(l => new string[3]
            //        {
            //            kvp.Key.sequence.Aggregate("", (c, n) => c == "" ? n.ToString() : c + "," + n.ToString()),  //seq
            //            l.id.ToString(),            //id
            //            l.confidence.ToString()     //confidence
            //        }));
            //    SQLiteHelper.InsertValues(dbname, "links", columns_links, links_data);
            //}
            SQLiteHelper.CreateIndex(dbname, "links", "seq_ind", "seq");
            SQLiteHelper.CreateIndex(dbname, "links", "seq_id_ind", "seq,id");
        }

        private Word StringsToWord(string s_id, string s_rank, string s_symbol, string s_childs, IEnumerable<string> s_parents)
        {
            int id = int.Parse(s_id);
            var rank = int.Parse(s_rank);
            string symbol = s_symbol;
            int[] childs = StringToIntArray(s_childs);
            int[] parents = s_parents.Select(p => int.Parse(p)).ToArray();
            return new Word(id, rank, symbol, childs, parents);
        }

        public IEnumerator<Word> GetEnumerator()
        {
            StringBuilder cmd_text = new StringBuilder();
            cmd_text.Append($"SELECT id,rank,symbol,childs FROM words");
            var reader = SQLiteHelper.CreateReader(db, cmd_text.ToString());
            while (reader.Read())
            {
                string[] row = new string[4];
                reader.GetValues(row);
                var parents_qry = SQLiteHelper.SelectValues(db,
                    tablename: "parents",
                    columns: "parent_id",
                    where: $"parents.id={row[0]}");
                IEnumerable<string> parents = parents_qry.Select(p => p[0]);
                yield return StringsToWord(
                    s_id: row[0],
                    s_rank: row[1],
                    s_symbol: row[2],
                    s_childs: row[3],
                    s_parents: parents);
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
    }

}
