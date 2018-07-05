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
        private string[] splitters;
        private int[] EmptyArray = new int[0];
        private int id_counter = 1;
        private Alphabet alphabet = new Alphabet();
        private Dictionary<int, Word> i2w = new Dictionary<int, Word>();
        private Dictionary<Word, int> w2i = new Dictionary<Word, int>();
        private Parser[] parsers;

        //Длина слова, используемая для преобразования лексикона в разреженную матрицу
        public static readonly int WORD_SIZE = 1024;
        //Размер буфера для чтения текста
        public static readonly int TEXT_BUFFER_SIZE = 1 << 28;


        public void Clear()
        {
            i2w.Clear();
            w2i.Clear();
            id_counter = 1;
            alphabet.Clear();
        }

        /// <summary>
        /// Создает словарь из потока
        /// </summary>
        /// <param name="streamreader">считыватель потока</param>
        /// <returns>количество созданных слов</returns>
        public int Create(StreamReader streamreader)
        {
            Console.WriteLine();
            this.Clear();
            int count_words = 0;
            char[] buffer = new char[Language.TEXT_BUFFER_SIZE];
            int count_chars = Language.TEXT_BUFFER_SIZE;
            int total_chars = 0;
            while (count_chars == Language.TEXT_BUFFER_SIZE)
            {
                count_chars = streamreader.ReadBlock(buffer, 0, Language.TEXT_BUFFER_SIZE);
                string text = new string(buffer, 0, count_chars);
                int current_chars = text.Length;
                total_chars += current_chars;
                Console.CursorLeft = 0;
                Console.Write($"Считано {current_chars}/{total_chars} байт ");
                count_words += this.Parse(text, this.Rank).Count();
            }
            return count_words;
        }

        /// <summary>
        /// Добавляет слова и возвращает количество добавленных слов
        /// </summary>
        /// <param name="text">строка текста для анализа и разбиения на слова</param>
        /// <returns>количество добавленных в лексикон слов</returns>
        public int Create(string text)
        {
            this.Clear();
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
