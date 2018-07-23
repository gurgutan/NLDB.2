using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public class DataContainer
    {
        private int block_size = 1 << 19;

        private SQLiteConnection db;

        private string[] splitters;
        private Alphabet alphabet = new Alphabet();
        private Dictionary<int, Word> i2w = new Dictionary<int, Word>();
        private Dictionary<Word, int> w2i = new Dictionary<Word, int>();
        private Dictionary<Sequence, Link[]> links = new Dictionary<Sequence, Link[]>();
        private string dbname = "data.db";

        public DataContainer(string _dbname)
        {
            dbname = _dbname;
        }

        public DataContainer(
            string[] _splitters,
            Alphabet _alphabet,
            Dictionary<int, Word> _i2w,
            Dictionary<Word, int> _w2i,
            Dictionary<Sequence, Link[]> _links)
        {
            splitters = _splitters;
            alphabet = _alphabet;
            i2w = _i2w;
            w2i = _w2i;
            links = _links;
        }

        public void Save(string _dbname)
        {
            dbname = _dbname;
            SaveSplitters();
            SaveAlphabet();
            SaveWords();
            SaveLinks();
        }

        private void SaveSplitters()
        {
            string[] columns = new string[] { "rank", "expr" };
            var data = splitters.Select((s, i) => new string[2] { s, i.ToString() });
            SQLiteHelper.CreateTable(dbname, "alphabet", columns, true);
            SQLiteHelper.InsertValues(dbname, "alphabet", columns, data);
        }

        private void SaveAlphabet()
        {
            string[] columns = new string[] { "code", "letter" };
            var data = alphabet.Letters.Select(kvp => new string[2] { kvp.Key.ToString(), kvp.Value });
            SQLiteHelper.CreateTable(dbname, "alphabet", columns, true);
            SQLiteHelper.InsertValues(dbname, "alphabet", columns, data);
            SQLiteHelper.CreateIndex(dbname, "alphabet", "code_ind", new string[] { "code" });
            SQLiteHelper.CreateIndex(dbname, "alphabet", "letter_ind", new string[] { "letter" });
        }

        private void SaveWords()
        {
            string[] columns_words = new string[] { "id", "rank", "childs" };
            string[] columns_parents = new string[] { "id", "parent_id" };
            var words_data = i2w.Values.Select(w => new string[3]
            {
                w.id.ToString(),            //id
                w.rank.ToString(),          //rank
                IntArrayToString(w.childs)  //childs
            }).Distinct();
            var parents_data = i2w.Values.SelectMany(w => w.parents.Select(p => new string[2] { w.id.ToString(), p.ToString() }));
            //Создаем таблицы
            SQLiteHelper.CreateTable(dbname, "words", columns_words, true);
            SQLiteHelper.CreateTable(dbname, "parents", columns_parents, true);
            //Добавляем данные
            SQLiteHelper.InsertValues(dbname, "words", columns_words, words_data);
            SQLiteHelper.InsertValues(dbname, "parents", columns_parents, parents_data);
            //Создаем индексы
            SQLiteHelper.CreateIndex(dbname, "words", "words_id_ind", new string[] { "id" });
            SQLiteHelper.CreateIndex(dbname, "words", "childs_ind", new string[] { "childs" });
            SQLiteHelper.CreateIndex(dbname, "parents", "parents_id_ind", new string[] { "id" });
        }

        private void SaveLinks()
        {
            string[] columns_links = new string[] { "seq", "id", "confidence" };
            SQLiteHelper.CreateTable(dbname, "links", columns_links, true);
            for (int i = 0; i < links.Count / block_size + 1; i++)
            {
                //"Вырезаем" block_size элементов из links, начиная с элемента, следующего за ранее обработанным блоком
                var links_data = links.
                    Skip(i * block_size).
                    Take(block_size).
                    SelectMany(kvp => kvp.Value.
                    Select(l => new string[3]
                    {
                        kvp.Key.sequence.Aggregate("", (c, n) => c == "" ? n.ToString() : c + "," + n.ToString()),  //seq
                        l.id.ToString(),            //id
                        l.confidence.ToString()     //confidence
                    }));
                SQLiteHelper.InsertValues(dbname, "links", columns_links, links_data);
            }

            SQLiteHelper.CreateIndex(dbname, "links", "seq_ind", new string[] { "seq" });
        }

        public void Load(string _dbname)
        {
            dbname = _dbname;
            throw new NotImplementedException();
        }

        public SQLiteConnection Open(string _dbname)
        {
            dbname = _dbname;
            db = SQLiteHelper.OpenConnection(dbname);
            return db;
        }

        public void Close()
        {
            SQLiteHelper.CloseConnection(db);
        }

        public int GetSymbol(string s)
        {
            if (db == null || db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            var letter = SQLiteHelper.SelectScalar(db,
                tablename: "alphabet",
                columns: "code",
                where: $"letter={s}");
            return (letter == null) ? 0 : (int)letter;
        }

        public string GetSymbol(int i)
        {
            if (db == null || db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            var code = SQLiteHelper.SelectScalar(db,
                tablename: "alphabet",
                columns: "code",
                where: $"code={i}");
            return (code == null) ? "" : (string)code;
        }

        public bool ContainsSymbol(string s)
        {
            return GetSymbol(s) != 0;
        }

        public bool ContainsSymbol(int i)
        {
            return GetSymbol(i) != null;
        }

        public Word Get(int i)
        {
            if (db == null || db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            //Поиск в БД
            var word = SQLiteHelper.SelectValues(db,
                tablename: "words",
                columns: "id,rank,childs",
                where: $"id={i}",
                limit: "").FirstOrDefault();
            if (word == null) return null;
            var rank = int.Parse(word[1]);
            int[] childs = StringToIntArray(word[2]);
            //Родительские
            var parents_qry = SQLiteHelper.SelectValues(db,
                tablename: "parents",
                columns: "id,parent_id",
                where: $"id={i}");
            int[] parents = parents_qry.Select(s => int.Parse(s[1])).ToArray();
            //Создание слова
            Word w = new Word(i, rank, childs, parents);
            return w;
        }

        public Word Get(int[] _childs)
        {
            if (db == null || db.State != System.Data.ConnectionState.Open)
                throw new Exception($"Подключение к БД не установлено");
            if (_childs == null || _childs.Length == 0)
                return null;
            //Получаем строкове представление childs
            var childs_str = @"""" + IntArrayToString(_childs) + @"""";
            //Поиск
            var word = SQLiteHelper.SelectValues(db,
                tablename: "words",
                columns: "id,rank,childs",
                where: $"childs={childs_str}").FirstOrDefault();
            if (word == null) return null;
            //id
            int id = int.Parse(word[0]);
            //Ранг
            int rank = int.Parse(word[1]);
            //Родительские
            var parents_qry = SQLiteHelper.SelectValues(db,
                tablename: "parents",
                columns: "id,parent_id",
                where: $"id={id}");
            int[] parents = parents_qry.Select(s => int.Parse(s[1])).ToArray();

            Word w = new Word(id, rank, _childs, parents);
            return w;
        }

        private string IntArrayToString(int[] a)
        {
            return a.Aggregate("", (c, n) => c == "" ? n.ToString() : c + "," + n.ToString());
        }

        private int[] StringToIntArray(string s)
        {
            return s.Split(separator: new char[] { ',' }, options: StringSplitOptions.RemoveEmptyEntries).Select(e => int.Parse(e)).ToArray();
        }
    }

}
