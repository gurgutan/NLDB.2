using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    [Serializable]
    public partial class Language
    {
        public static int WORD_SIZE = 1024;
        private readonly int bufferSize = 1 << 28;
        public readonly string Name;
        public int Rank;

        public List<Lexicon> Lexicons = new List<Lexicon>();

        public Language(string name, string[] splitters)
        {
            this.Name = name;
            this.Rank = splitters.Length - 1;
            this.Init(splitters);
        }

        public void Clear()
        {
            Lexicons.ForEach(l => l.Clear());
            Lexicons.Clear();
        }

        public Lexicon this[int r]
        {
            get { return this.Lexicons[r]; }
        }

        public Term Evaluate(string s, int rank = 1)
        {
            rank = Math.Min(this.Rank, rank);
            return this.Lexicons[rank].Evaluate(s);
        }

        public void EvaluateTerm(Term term)
        {
            if (term.Rank < 0 || term.Rank > this.Rank)
                throw new ArgumentOutOfRangeException($"Ранг терма выходит за границы допустимых значений данного языка: [0,{this.Rank}]");
            this.Lexicons[term.Rank].Evaluate(term);
        }

        public IEnumerable<Term> FindMany(string s, int count = 0, int rank = 1)
        {
            rank = Math.Min(this.Rank, rank);
            Term term = this.Lexicons[rank].BuildTerm(s);
            return this.Lexicons[rank].FindMany(term, count);
        }

        public int CreateFromTextFile(string filename)
        {
            if (!File.Exists(filename)) throw new FileNotFoundException("Файл не найден");
            int wordscount = 0;
            using (StreamReader file = File.OpenText(filename))
            {
                Console.WriteLine($"\nОбработка файла '{filename}'");
                char[] buffer = new char[this.bufferSize];
                int count = this.bufferSize;
                int total = 0;
                while (count == this.bufferSize)
                {
                    count = file.ReadBlock(buffer, 0, this.bufferSize);
                    total += count;
                    string text = new string(buffer, 0, count);
                    Console.Write($"Считана строка длины {count}. Всего {total} байт ");
                    Console.CursorLeft = 0;
                    wordscount += this.Lexicons[this.Rank].TryAddMany(text).Length;
                }
                Console.WriteLine("\nОбработка файла завершена");
            }
            return wordscount;
        }

        /// <summary>
        /// Добавляет слова в лексикон и возвращает количество добавленных слов
        /// </summary>
        /// <param name="text">строка текста для анализа и разбиения на слова</param>
        /// <returns>количество добавленных в лексикон слов</returns>
        public int CreateFromString(string text)
        {
            return this.Lexicons[this.Rank].TryAddMany(text).Length;
        }

        public void Seriailize(string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Create);
            BinaryFormatter formatter = new BinaryFormatter();
            try
            {
                formatter.Serialize(fs, this);
            }
            catch (SerializationException e)
            {
                Console.WriteLine("Ошибка сериализации: " + e.Message);
                throw;
            }
            finally
            {
                fs.Close();
            }
        }

        public static Language Deserialize(string filename)
        {
            Language language = null;
            FileStream fs = new FileStream(filename, FileMode.Open);
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                language = (Language)formatter.Deserialize(fs);
                return language;
            }
            catch (SerializationException e)
            {
                Console.WriteLine("Ошибка десериализации: " + e.Message);
                throw;
            }
            finally
            {
                fs.Close();
            }
        }

        private void Init(string[] splitters)
        {
            int n = splitters.Length;
            for (int i = 0; i < n; i++)
                this.Lexicons.Add(
                    new Lexicon(this, splitters[i], i > 0 ? this.Lexicons[i - 1] : null)
                    );
        }
    }
}
