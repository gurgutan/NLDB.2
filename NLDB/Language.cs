using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private DataContainer data;

        private int[] EmptyArray = new int[0];
        //private int id_counter = 1;
        private Parser[] parsers;

        private string[] splitters;

        private Grammar grammar = new Grammar();

        public string Name { get; private set; }

        public int Rank { get { return data.Splitters.Length - 1; } }

        public string[] Splitters { get { return this.data.Splitters; } }

        public int Count { get { return data.Count(); } }

        //Размер буфера для чтения текста
        public static readonly int TEXT_BUFFER_SIZE = 1 << 22;

        public Language(string _name, string[] _splitters)
        {
            this.Name = _name;
            splitters = _splitters;
            parsers = splitters.Select(s => new Parser(s)).ToArray();
            data = new DataContainer(_name, splitters);
            //data.Open(Name);
        }

        public void Connect(string dbname)
        {
            if (data.IsOpen()) data.Close();
            data = new DataContainer(dbname, splitters);
            data.Open(dbname);
        }

        public void Disconnect()
        {
            data.Close();
        }

        public Word Find(int i)
        {
            return data.Get(i);
        }

        public Word Find(int[] i)
        {
            return data.Get(i);
        }

        public void FreeMemory()
        {
            data.ClearCash();
        }

        public void New()
        {
            if (File.Exists(Name)) File.Delete(Name);
            data = new DataContainer(Name, splitters);
        }

        /// <summary>
        /// Создает словарь из потока
        /// </summary>
        /// <param name="streamreader">считыватель потока</param>
        /// <returns>количество созданных слов</returns>
        public int Build(StreamReader streamreader)
        {
            //data.Create();
            data.Open(Name);
            int words_count = BuildWords(streamreader) + BuildGrammar(); //BuildSequences();
            data.Close();
            return words_count;
        }

        private int BuildWords(StreamReader streamreader)
        {
            int count_words = 0;
            char[] buffer = new char[Language.TEXT_BUFFER_SIZE];
            int count_chars = Language.TEXT_BUFFER_SIZE;
            int total_chars = 0;
            while (count_chars == Language.TEXT_BUFFER_SIZE)
            {
                count_chars = streamreader.ReadBlock(buffer, 0, Language.TEXT_BUFFER_SIZE);
                string text = new string(buffer, 0, count_chars);
                total_chars += count_chars;
                Console.Write($"Считано {total_chars} символов."); Console.CursorLeft = 0;
                data.BeginTransaction();
                count_words += this.Parse(text, this.Rank).Count();
                data.EndTransaction();
            }
            //Очистка кэша
            data.ClearCash();
            return count_words;
        }

        public int BuildGrammar()
        {
            grammar.Clear();
            Debug.WriteLine("Построение грамматики");
            var words = data.Where(w => w.rank > 0);
            int count = 0;
            foreach (var w in words)
            {
                grammar.Add(w.childs);
                count++;
                Debug.WriteLineIf(count % (1 << 18) == 0, count);
            }
            return grammar.Count();
        }

        /// <summary>
        /// Добавляет слова и возвращает количество добавленных слов
        /// </summary>
        /// <param name="text">строка текста для анализа и разбиения на слова</param>
        /// <returns>количество добавленных в лексикон слов</returns>
        public int Build(string text)
        {
            return Parse(text, this.Rank).Count();
        }

        public void Serialize(string filename)
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

    }
}
