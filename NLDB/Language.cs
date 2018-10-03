using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

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
        private GrammarTree grammar = new GrammarTree();

        //Основные свойства
        public string Name { get; private set; }
        public int Rank => this.data.Splitters.Length - 1;
        public string[] Splitters => this.data.Splitters;
        public int Count => this.data.Count();
        //Размер буфера для чтения текста
        public static readonly int TEXT_BUFFER_SIZE = 1 << 16;

        public Language(string _name, string[] _splitters)
        {
            Name = _name;
            this.splitters = _splitters;
            this.parsers = this.splitters.Select(s => new Parser(s)).ToArray();
            this.data = new DataContainer(_name, this.splitters);
        }

        //--------------------------------------------------------------------------------------------
        //Методы работы с базой данных
        //--------------------------------------------------------------------------------------------
        public void Connect()
        {
            if (this.data.IsOpen()) this.data.CloseConnection();
            this.data = new DataContainer(Name, this.splitters);
            this.data.Connect(Name);
        }

        public void Disconnect()
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
            if (this.data.IsOpen()) this.data.CloseConnection();
            if (File.Exists(Name)) File.Delete(Name);
            this.data = new DataContainer(Name, this.splitters);
            this.data.CreateDB();
        }

        /// <summary>
        /// Присваивает Name новое имя dbname и создает одноименную базу данных лексикона
        /// </summary>
        /// <param name="_dbname"></param>
        public void CreateDB(string _dbname)
        {
            Name = _dbname;
            CreateDB();
        }

        //--------------------------------------------------------------------------------------------
        //Методы работы со словами
        //--------------------------------------------------------------------------------------------
        public Word Find(int i)
        {
            return this.data.Get(i);
        }

        public Word Find(int[] i)
        {
            return this.data.Get(i);
        }

        public Term ToTerm(string text, int rank)
        {
            text = this.parsers[rank].Normilize(text);
            return new Term(rank, 0, 0, text,
                rank == 0 ? null :
                this.parsers[rank - 1].
                Split(text).
                Where(s => !string.IsNullOrWhiteSpace(s)).
                Select(s => ToTerm(s, rank - 1)));
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
            var result = strings.Select(s =>
            {
                int id;
                int[] childs = null;
                if (rank > 0)
                {
                    childs = Parse(s, rank - 1).ToArray();      //получаем id дочерних слов ранга rank-1
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
            }).Where(i => i != 0).ToList();
            return result;
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
                term.id = this.data.GetId(term.text);
                term.confidence = (term.id == 0 ? 0 : 1);
                term.Identified = true;
                return term;
            }
            else
            {
                //Если ранг терма больше нуля, то confidence считаем по набору дочерних элементов
                Stopwatch sw = new Stopwatch(); //!!!
                sw.Start(); //!!!
                int[] childs = term.Childs.
                    Distinct(new TermComparer()).
                    Select(c => Identify(c)).
                    Where(c => c.id != 0).
                    Select(c => c.id).
                    ToArray();
                sw.Stop();  //!!!
                Debug.WriteLine($"Identify->{term.ToString()}.childs [{childs.Length}]: {sw.Elapsed.TotalSeconds}");
                sw.Restart(); //!!!
                List<int> parents = this.data.GetParentsId(childs).ToList();
                sw.Stop();  //!!!
                Debug.WriteLine($"Identify->{term.ToString()}.parents [{parents.Count}]: {sw.Elapsed.TotalSeconds}");
                sw.Restart(); //!!!
                List<Term> context = parents.Select(p => ToTerm(p)).ToList();
                sw.Stop();  //!!!
                Debug.WriteLine($"Identify->{term.ToString()}.context [{context.Count}]: {sw.Elapsed.TotalSeconds}");
                //Поиск ближайшего родителя, т.е.родителя с максимумом сonfidence
                Link max = context.AsParallel().Aggregate(
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
            Term term = ToTerm(text, rank);
            return Identify(term);
        }

        /// <summary>
        /// Метод ищет схожие с текстом text слова в лексиконе и возвращает их представление в виде списка Термов
        /// </summary>
        /// <param name="text"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        public List<Term> Similars(string text, int rank = 2)
        {
            text = this.parsers[rank].Normilize(text);
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
            int[] childs = term.Childs.
                Where(c => c.id != 0).
                Select(c => c.id).
                Distinct().
                ToArray();
            sw.Stop();  //!!!
            Debug.WriteLine($"Similars->childs [{childs.Length}]: {sw.Elapsed.TotalSeconds}");
            sw.Restart(); //!!!
            List<Term> context = this.data.
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
            List<Term> terms = Parse(text, rank).
                Select(t => ToTerm(t)).
                ToList();
            sw.Stop();  //!!!
            Debug.WriteLine($"Парсинг: {sw.Elapsed.TotalSeconds}");
            sw.Restart(); //!!!
            IEnumerable<Term> similars = Similars(text, rank).Where(t => t.confidence >= similars_min_confidence).Take(similars_max_count);
            sw.Stop();  //!!!
            Debug.WriteLine($"Определение similars [{similars.Count()}]: {sw.Elapsed.TotalSeconds}");
            similars.ToList().ForEach(s => Debug.WriteLine($" [{s.confidence.ToString("F4")}] {s}"));    //!!!
            if (similars.Count() == 0) return result;
            //запоминаем веса 
            Dictionary<int, float> weights = similars.ToDictionary(s => s.id, s => s.confidence);
            sw.Restart(); //!!!
            //Получаем контекст
            Dictionary<int, Link> context = this.data.
                GetParentsWithChilds(weights.Keys.ToArray()).
                SelectMany(p => this.data.GetGrandchildsId(p.Item2.id).
                Select(gc => new Link(gc, 0, weights[p.Item1]))).
                Distinct(new LinkComparer()).
                ToDictionary(link => link.id, link => link);
            sw.Stop();  //!!!
            Debug.WriteLine($"Определение context [{context.Count}]: {sw.Elapsed.TotalSeconds}");
            //Запоминаем словарь допустимых слов, для использования в поиске
            sw.Restart(); //!!!
            context.Add(this.grammar.Root.id, new Link(0, 0, 0));
            Tuple<float, Stack<Link>> path = FindSequence(this.grammar.Root, context);
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
            foreach (Rule t in rule.Rules)
            {
                //if (!bag.ContainsKey(t.Key)) continue;
                Tuple<float, Stack<Link>> cur_path = FindSequence(t, context);
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
            this.data.ClearCash();
        }

        //--------------------------------------------------------------------------------------------
        //Методы построения лексикона, грамматики
        //--------------------------------------------------------------------------------------------
        /// <summary>
        /// Создает словарь из потока
        /// </summary>
        /// <param name="streamreader">считыватель потока</param>
        /// <returns>количество созданных слов</returns>
        public void BuildLexicon(string filename)
        {
            if (!this.data.IsOpen())
                throw new Exception($"Нет подключения к базе данных!");
            LearnWords(filename);
            BuildGrammar(); //BuildSequences();
        }

        private void LearnWords(string filename)
        {
            char[] buffer = new char[Language.TEXT_BUFFER_SIZE];
            int count_chars = Language.TEXT_BUFFER_SIZE;
            using (StreamReader reader = File.OpenText(filename))
            {
                ProgressInformer informer = new ProgressInformer("Извлечение слов:", reader.BaseStream.Length);
                while (count_chars == TEXT_BUFFER_SIZE)
                {
                    count_chars = reader.ReadBlock(buffer, 0, Language.TEXT_BUFFER_SIZE);
                    string text = new string(buffer, 0, count_chars);
                    this.data.BeginTransaction();
                    Parse(text, Rank);
                    this.data.EndTransaction();
                    informer.Current = reader.BaseStream.Position;
                    informer.Show();
                }
                //Очистка кэша
                this.data.ClearCash();
                Console.WriteLine($"\nСчитано {informer.Current} символов. Добавлено {this.data.Count()} слов.");
            }
        }

        public void BuildGrammar()
        {
            this.grammar.Clear();
            IEnumerable<Word> words = this.data.Where(w => w.rank > 0);
            ProgressInformer informer = new ProgressInformer("Построение грамматики:", words.Count());
            int count = 0;
            foreach (Word w in words)
            {
                this.grammar.Add(w.childs);
                count++;
                informer.Current = count;
                if (count % (1 << 8) == 0) informer.Show();
                //очистка кэша на каждые n слов
                if (count % (1 << 20) == 0) this.data.ClearCash();
                //Debug.WriteLineIf(count % (1 << 18) == 0, count);
            }
            informer.Set(count);
        }

        /// <summary>
        /// Добавляет слова и возвращает количество добавленных слов
        /// </summary>
        /// <param name="text">строка текста для анализа и разбиения на слова</param>
        /// <returns>количество добавленных в лексикон слов</returns>
        public int Build(string text)
        {
            return Parse(text, Rank).Count();
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
