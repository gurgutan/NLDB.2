using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public class Lexicon
    {
        private readonly Parser parser;

        public int Rank;
        public string Splitter = "";

        public Lexicon Child;
        public Lexicon Parent;
        readonly Dictionary<Word, int> words = new Dictionary<Word, int>();
        readonly Dictionary<string, int> alphabet = new Dictionary<string, int>();


        public int Count
        {
            get { return this.words.Count; }
        }

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

        public int[] Codes
        {
            get
            {
                return this.words.Values.ToArray();
            }
        }

        public int[] ChildCodes
        {
            get
            {
                if (this.Child == null)
                    return null;
                return this.Child.Codes;
            }
        }

        public string[] Alphabet
        {
            get
            {
                return this.alphabet.Keys.ToArray();
            }
        }

        /// <summary>
        /// Возвращает код слова, представленного строкой s
        /// </summary>
        /// <param name="s">тектсовое представление слова</param>
        /// <returns>код, идентифицирующий слово или -1, если слово с текстовым представлением s не найдено в словаре</returns>
        public int AsCode(string s)
        {
            if (this.alphabet.TryGetValue(s, out int code))
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
                if ((id = this.AsCode(s)) == -1)
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
            int id = this.words.Count;
            this.words.Add(new Word(id, new int[0]), id);
            this.alphabet.Add(s, id);
            return id;
        }

        /// <summary>
        /// Добавляет составное слово
        /// </summary>
        /// <param name="subwords">id дочерних слов</param>
        /// <returns>код добавленного слова</returns>
        public int Add(int[] subwords)
        {
            int id = this.words.Count;
            this.words.Add(new Word(id, subwords), id);
            return id;
        }

        public int Find(int[] subwords)
        {
            if (this.words.TryGetValue(new Word(-1, subwords), out int id))
                return id;
            return -1;
        }

    }
}
