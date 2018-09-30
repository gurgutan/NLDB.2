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
    /// <summary>
    /// Класс представляющий сущность Язык. 
    /// Данные: Слова, Парсеры, Сплиттеры, Грамматика.
    /// Методы: методы работы с базой данных, 
    /// </summary>
    [Serializable]
    public partial class Language
    {
        //Данные
        private DataContainer data;
        private Parser[] parsers;
        private string[] splitters;
        private Grammar grammar = new Grammar();

        //Основные свойства
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
        }

        //--------------------------------------------------------------------------------------------
        //Методы работы с базой данных
        //--------------------------------------------------------------------------------------------
        public void ConnectDB()
        {
            if (data.IsOpen()) data.CloseConnection();
            data = new DataContainer(this.Name, this.splitters);
            data.Connect(this.Name);
        }

        public void DisconnectDB()
        {
            this.data.CloseConnection();
        }

        public bool IsConnected()
        {
            return this.data.IsOpen();
        }

        /// <summary>
        /// Создает базу данных лексикона с именем Name
        /// </summary>
        public void CreateDB()
        {
            if (data.IsOpen()) data.CloseConnection();
            if (File.Exists(Name)) File.Delete(this.Name);
            data = new DataContainer(this.Name, this.splitters);
            data.CreateDB();
        }

        /// <summary>
        /// Присваивает Name новое имя dbname и создает одноименную базу данных лексикона
        /// </summary>
        /// <param name="_dbname"></param>
        public void CreateDB(string _dbname)
        {
            this.Name = _dbname;
            this.CreateDB();
        }

        //--------------------------------------------------------------------------------------------
        //Методы работы со словами
        //--------------------------------------------------------------------------------------------
        public Word Find(int i)
        {
            return data.Get(i);
        }

        public Word Find(int[] i)
        {
            return data.Get(i);
        }

        public Term ToTerm(string text, int rank)
        {
            text = this.parsers[rank].Normilize(text);
            return new Term(rank, 0, 0, text,
                rank == 0 ? null :
                this.parsers[rank - 1].
                Split(text).
                Where(s => !string.IsNullOrWhiteSpace(s)).
                Select(s => this.ToTerm(s, rank - 1)));
        }

        public Term ToTerm(Word w, float _confidence = 1)
        {
            return this.data.ToTerm(w, _confidence);
        }

        public Term ToTerm(int i, float _confidence = 1)
        {
            return this.data.ToTerm(i, _confidence);
        }

        //--------------------------------------------------------------------------------------------
        //Методы обработки и анализа текста
        //--------------------------------------------------------------------------------------------
        private IEnumerable<int> Parse(string text, int rank)
        {
            text = this.parsers[rank].Normilize(text);
            IEnumerable<string> strings = this.parsers[rank].Split(text).Where(s => !string.IsNullOrEmpty(s));
            //Для слов ранга > 0 добавляем слова, которых еще нет
            return strings.Select(s =>
            {
                int id;
                int[] childs = null;
                if (rank > 0)
                {
                    childs = this.Parse(s, rank - 1).ToArray();      //получаем id дочерних слов ранга rank-1
                    if (childs.Length == 0) return 0;
                    id = this.data.GetId(childs);
                    if (id == 0) id = this.data.Add(new Word(0, rank, "", childs, new int[0]));
                }
                else
                {
                    id = this.data.GetId(s);
                    if (id == 0) id = this.data.Add(new Word(0, rank, s, null, new int[0]));
                }
                return id;
            }).Where(i => i != 0);
        }


        /// <summary>
        /// Вычисляет значение confidence и id для терма term. Меняет переданный по ссылке term
        /// </summary>
        /// <param name="term">изменяемый терм</param>
        /// <returns>возвращает ссылку на term (возврат значения для удобства использования в LINQ)</returns>
        public Term Identify(Term term)
        {
            if (term.Identified) return term;
            if (term.rank == 0)
            {
                //При нулевом ранге терма, confidence считаем исходя из наличия соответствующей буквы в алфавите
                term.id = data.GetId(term.text);
                term.confidence = (term.id == 0 ? 0 : 1);
                term.Identified = true;
                return term;
            }
            else
            {
                //Если ранг терма больше нуля, то confidence считаем по набору дочерних элементов
                Stopwatch sw = new Stopwatch(); //!!!
                sw.Start(); //!!!
                var childs = term.Childs.
                    Distinct(new TermComparer()).
                    Select(c => Identify(c)).
                    Where(c => c.id != 0).
                    Select(c => c.id).
                    ToArray();
                sw.Stop();  //!!!
                Debug.WriteLine($"Identify->{term.ToString()}.childs [{childs.Length}]: {sw.Elapsed.TotalSeconds}");
                sw.Restart(); //!!!
                var parents = data.GetParentsId(childs).ToList();
                sw.Stop();  //!!!
                Debug.WriteLine($"Identify->{term.ToString()}.parents [{parents.Count}]: {sw.Elapsed.TotalSeconds}");
                sw.Restart(); //!!!
                var context = parents.Select(p => ToTerm(p)).ToList();
                sw.Stop();  //!!!
                Debug.WriteLine($"Identify->{term.ToString()}.context [{context.Count}]: {sw.Elapsed.TotalSeconds}");
                //Поиск ближайшего родителя, т.е.родителя с максимумом сonfidence
                var max = context.AsParallel().Aggregate(
                    new Link(),
                    (subtotal, thread_term) =>
                    {
                        float confidence = Confidence.Compare(term, thread_term);
                        if (subtotal.confidence < confidence) return new Link(thread_term.id, 0, confidence);
                        return subtotal;
                    },
                    (total, subtotal) => total.confidence < subtotal.confidence ? subtotal : total,
                    (final) => final);
                term.id = max.id;
                term.confidence = max.confidence;
                term.Identified = true;
                return term;
            }
        }

        /// <summary>
        /// Метод ищет и возвращает Терм, построенный из одного из Слов Лексикона, наиболее похожего на текст text
        /// </summary>
        /// <param name="text"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        public Term Similar(string text, int rank)
        {
            text = this.parsers[rank].Normilize(text);
            Term term = this.ToTerm(text, rank);
            return this.Identify(term);
        }

        /// <summary>
        /// Метод ищет схожие с текстом text слова в лексиконе и возвращает их представление в виде списка Термов
        /// </summary>
        /// <param name="text"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        public List<Term> Similars(string text, int rank = 2)
        {
            text = parsers[rank].Normilize(text);
            Stopwatch sw = new Stopwatch(); //!!!
            sw.Start(); //!!!
            Term term = ToTerm(text, rank);
            sw.Stop();  //!!!
            Debug.WriteLine($"Similars->ToTerm: {sw.Elapsed.TotalSeconds}");
            sw.Restart(); //!!!
            Identify(term);
            sw.Stop();  //!!!
            Debug.WriteLine($"Similars->Identify: {sw.Elapsed.TotalSeconds}");
            //Для терма нулевого ранга возвращаем результат по наличию соответствующей буквы в алфавите
            if (term.rank == 0) return new List<Term> { term };
            //Определение контекста по дочерним словам
            sw.Restart(); //!!!
            var childs = term.Childs.
                Where(c => c.id != 0).
                Select(c => c.id).
                Distinct().
                ToArray();
            sw.Stop();  //!!!
            Debug.WriteLine($"Similars->childs [{childs.Length}]: {sw.Elapsed.TotalSeconds}");
            sw.Restart(); //!!!
            var context = data.
                GetParentsId(childs).
                Distinct().
                Select(p => ToTerm(p)).
                ToList();
            sw.Stop();  //!!!
            Debug.WriteLine($"Similars->context [{context.Count}]: {sw.Elapsed.TotalSeconds}");
            //Расчет оценок Confidence для каждого из соседей
            sw.Restart(); //!!!
            context.AsParallel().ForAll(p => p.confidence = Confidence.Compare(term, p));
            sw.Stop();  //!!!
            Debug.WriteLine($"Similars->Compare [{context.Count}]: {sw.Elapsed.TotalSeconds}");
            //Сортировка по убыванию оценки
            context.Sort(new Comparison<Term>((t1, t2) => Math.Sign(t2.confidence - t1.confidence)));
            return context.ToList();
        }

        /// <summary>
        /// Метод определяет следующее Слово, которое подходит как продолжение текста text
        /// </summary>
        /// <param name="text"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        public List<Term> Next(string text, int rank = 2)
        {
            List<Term> result = new List<Term>();
            Stopwatch sw = new Stopwatch(); //!!!
            sw.Start(); //!!!
            var terms = Parse(text, rank).
                Select(t => ToTerm(t)).
                ToList();
            sw.Stop();  //!!!
            Debug.WriteLine($"Парсинг: {sw.Elapsed.TotalSeconds}");
            sw.Restart(); //!!!
            var similars = Similars(text, rank).Where(t => t.confidence >= similars_min_confidence).Take(similars_max_count);
            sw.Stop();  //!!!
            Debug.WriteLine($"Определение similars [{similars.Count()}]: {sw.Elapsed.TotalSeconds}");
            similars.ToList().ForEach(s => Debug.WriteLine($" [{s.confidence.ToString("F4")}] {s}"));    //!!!
            if (similars.Count() == 0) return result;
            //запоминаем веса 
            Dictionary<int, float> weights = similars.ToDictionary(s => s.id, s => s.confidence);
            sw.Restart(); //!!!
            //Получаем контекст
            var context = data.
                GetParentsWithChilds(weights.Keys.ToArray()).
                SelectMany(p => data.GetGrandchildsId(p.Item2.id).
                Select(gc => new Link(gc, 0, weights[p.Item1]))).
                Distinct(new LinkComparer()).
                ToDictionary(link => link.id, link => link);
            sw.Stop();  //!!!
            Debug.WriteLine($"Определение context [{context.Count}]: {sw.Elapsed.TotalSeconds}");
            //Запоминаем словарь допустимых слов, для использования в поиске
            sw.Restart(); //!!!
            context.Add(grammar.Root.id, new Link(0, 0, 0));
            var path = FindSequence(grammar.Root, context);
            sw.Stop();  //!!!
            Debug.WriteLine($"Определение path [{path.Item1.ToString("F4")};{path.Item2.Count}]: {sw.Elapsed.TotalSeconds}");
            return path.Item2.Skip(1).Select(link => ToTerm(link.id)).ToList();
        }

        /// <summary>
        /// Метод достраивает релевантную цепочку по исходному правилу rule
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public Tuple<float, Stack<Link>> FindSequence(Rule rule, Dictionary<int, Link> context)
        {
            if (!context.ContainsKey(rule.id)) return null;
            float head_weight = context[rule.id].confidence;
            float tail_weight = 0;
            Stack<Link> path = new Stack<Link>();
            Link found = new Link();
            foreach (var t in rule.Rules)
            {
                //if (!bag.ContainsKey(t.Key)) continue;
                var cur_path = FindSequence(t, context);
                if (cur_path == null) continue;
                if (tail_weight < cur_path.Item1)
                {
                    //критерий определяется как максимум сумм произведений вероятности слова в цепочке на confidence слова
                    found = new Link(t.id, 0, context[t.id].confidence * rule.Confidence(t.id));
                    tail_weight = cur_path.Item1;
                    path = cur_path.Item2;
                }
            }
            path.Push(new Link(rule.id, 0, head_weight));
            return new Tuple<float, Stack<Link>>((head_weight + tail_weight) / (path.Count / attenuation), path);
        }

        //--------------------------------------------------------------------------------------------
        //Работа с кэшем
        //--------------------------------------------------------------------------------------------
        protected void FreeMemory()
        {
            data.ClearCash();
        }

        //--------------------------------------------------------------------------------------------
        //Методы построения лексикона, грамматики
        //--------------------------------------------------------------------------------------------
        /// <summary>
        /// Создает словарь из потока
        /// </summary>
        /// <param name="streamreader">считыватель потока</param>
        /// <returns>количество созданных слов</returns>
        public int BuildLexicon(StreamReader streamreader)
        {
            //data.Create();
            //data.Connect(Name);
            if (!data.IsOpen())
                throw new Exception($"Нет подключения к базе данных!");
            int words_count = BuildWords(streamreader) + BuildGrammar(); //BuildSequences();
            //data.Close();
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
                //очистка кэша на каждом миллионе слов
                if (count % (1 << 20) == 0) data.ClearCash();
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

        //--------------------------------------------------------------------------------------------
        //Методы сериализации
        //--------------------------------------------------------------------------------------------
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

        //--------------------------------------------------------------------------------------------
        //Служебные структуры данных и параметры алгоритмов
        //--------------------------------------------------------------------------------------------
        //Параметры алгоритмов поиска
        private const int similars_max_count = 8;           //максимальное количество похожих слов при поиске
        private const float attenuation = 100F;          //количество слов в ответе, которое обнуляет confidence ответа
        private const float similars_min_confidence = 0.5F;
        private const float followers_min_confidence = 0.02F;
        private const int followers_max_count = 16;
        //private int[] EmptyArray = new int[0];
    }
}
