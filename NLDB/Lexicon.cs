using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public struct TripleInt
    {
        public int val;
        public int row;
        public int col;

        public TripleInt(int v, int r, int c)
        {
            val = v;
            row = r;
            col = c;
        }

        public static int Compare(TripleInt a, TripleInt b)
        {
            if (a.row > b.row) return 1;
            if (a.row < b.row) return -1;
            if (a.col > b.col) return 1;
            if (a.col < b.col) return -1;
            return 0;
        }
    }

    public class Lexicon
    {
        private readonly Parser parser;

        public int Rank;
        public string Splitter = "";

        public Lexicon Child;
        public Lexicon Parent;
        readonly Dictionary<Word, int> words = new Dictionary<Word, int>();
        readonly Dictionary<int, Word> iwords = new Dictionary<int, Word>();
        readonly Dictionary<string, int> alphabet = new Dictionary<string, int>();
        readonly Dictionary<int, string> ialphabet = new Dictionary<int, string>();


        public Lexicon(string splitter, Lexicon child = null, Lexicon parent = null)
        {
            if (child == null)
                this.Rank = 0;
            else
                this.Rank = child.Rank + 1;
            this.Child = child;
            this.Parent = parent;
            this.Splitter = splitter;
            this.parser = new Parser(this.Splitter);
        }

        public int Count
        {
            get { return this.words.Count; }
        }

        /// <summary>
        /// Массив Id слов
        /// </summary>
        public IEnumerable<int> Codes
        {
            get
            {
                return this.words.Values.ToArray();
            }
        }

        public IEnumerable<Word> Words
        {
            get
            {
                return words.Keys;
            }
        }

        /// <summary>
        /// Массив базовых строковых представлений символов(букв) словаря. Имеет смысл только для словаря ранга 0, так как 
        /// </summary>
        public IEnumerable<string> Alphabet
        {
            get
            {
                return this.alphabet.Keys;
            }
        }

        public string AsText(int i)
        {
            if (this.Rank == 0)
                return this.ialphabet[i];
            return this.iwords[i].childs.Aggregate("",
                (c, n) => c + this.Child.AsText(n));
        }

        /// <summary>
        /// Возвращает код слова, представленного строкой s
        /// </summary>
        /// <param name="s">тектсовое представление слова</param>
        /// <returns>код, идентифицирующий слово или -1, если слово с текстовым представлением s не найдено в словаре</returns>
        public int AsCode(string s)
        {
            int code;
            if (this.alphabet.TryGetValue(s, out code))
                return code;
            return -1;
        }

        public int[] TryAddMany(string text)
        {
            string normilizedText = this.parser.Normilize(text);
            return
                this.parser.
                Split(normilizedText).
                Where(s => !string.IsNullOrEmpty(s)).
                Select(s => this.TryAdd(s)).
                ToArray();
        }

        /// <summary>
        /// Пытается добавить слово, представленное строкой s, в словарь. Если такое слово уже есть, то добавления не происходит.
        /// </summary>
        /// <param name="s">текстовое предстваление слова</param>
        /// <returns>возвращает id слова</returns>
        public int TryAdd(string s)
        {
            int id = -1;
            if (this.Rank == 0)
            {
                if ((id = this.Find(s)) == -1)
                    id = this.Add(s);
            }
            else
            {
                int[] subwords = this.Child.TryAddMany(s);
                if ((id = this.Find(subwords)) == -1)
                    id = this.Add(subwords);
            }
            return id;
        }

        /// <summary>
        /// Добавляет атомарное слово, представленное строкой s
        /// </summary>
        /// <param name="s">тектсовое представление слова</param>
        /// <returns>возвращает код добавленного слова</returns>
        public int Add(string s)
        {
            return this.Register(new Word(-1, new int[0]), s);
        }

        /// <summary>
        /// Добавляет составное слово
        /// </summary>
        /// <param name="subwords">id дочерних слов</param>
        /// <returns>код добавленного слова</returns>
        public int Add(int[] subwords)
        {
            return this.Register(new Word(-1, subwords));
        }

        public int Find(int[] subwords)
        {
            int id;
            if (this.words.TryGetValue(new Word(-1, subwords), out id))
                return id;
            return -1;
        }

        public int Find(string s)
        {
            int id;
            if (this.alphabet.TryGetValue(s, out id))
                return id;
            return -1;
        }

        /// <summary>
        /// Возвращает представление словаря ранга r, как матрицы размерности m x n, где m - размер дочернего словаря, а n - размер словаря r
        /// </summary>
        /// <param name="r">ранга словаря</param>
        /// <returns></returns>
        public int[,] AsDenseMatrix()
        {
            if (this.Rank == 0) throw new ArgumentOutOfRangeException("Для словаря нулевого ранга не существует матричного представления");
            int[,] matrix = new int[this.Child.Count, this.Count];
            int col = 0;
            foreach (var w in words.Keys)
            {
                for (int i = 0; i < w.childs.Length; i++)
                {
                    matrix[w.childs[i], col] |= (1 << i);
                }
                col++;
            }
            return matrix;
        }

        /// <summary>
        /// Метод формирует матрицу в координатном формате
        /// </summary>
        /// <returns>массив из трех массивов: [0] - значения, [1] - индексы строк, [2] - индексы колонок</returns>
        public int[][] AsCOOMatrix()
        {
            if (this.Rank == 0) throw new ArgumentOutOfRangeException("Для словаря нулевого ранга не существует матричного представления");
            List<TripleInt> values = new List<TripleInt>();
            int col = 0;
            foreach (var w in words.Keys)
            {
                //Составляем набор значений для записи в матрицу
                Dictionary<int, int> bitmap = new Dictionary<int, int>(w.childs.Length);
                int pos = 0;
                foreach (var row in w.childs)
                {
                    if (!bitmap.ContainsKey(row))
                        bitmap[row] = (1 << pos);
                    else
                        bitmap[row] |= (1 << pos);
                    pos++;
                }
                //Добавляем значения в списки для COOMatrix
                foreach(var v in bitmap)
                {
                    values.Add(new TripleInt(v.Value, v.Key, col));
                }
                col++;
            }
            values.Sort((a, b) => TripleInt.Compare(a, b));
            int[][] matrix = new int[3][];
            matrix[0] = values.Select(v => v.val).ToArray();
            matrix[1] = values.Select(v => v.row).ToArray();
            matrix[2] = values.Select(v => v.col).ToArray();
            return matrix;
        }


        private int Register(Word w, string s = "")
        {
            int id = this.words.Count;
            //Заменяем id слова w, на новый
            w.id = id;
            if (!string.IsNullOrEmpty(s))
            {
                this.alphabet.Add(s, id);
                this.ialphabet.Add(id, s);
            }
            this.words.Add(w, w.id);
            this.iwords.Add(w.id, w);
            return id;
        }


    }
}
