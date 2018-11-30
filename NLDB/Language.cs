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
        public enum ProcessingType { Build, Grammar, Distance };
        //Данные
        private DataContainer data = null;
        private Parser[] parsers = null;
        private string[] splitters = null;
        private GrammarTree grammar = new GrammarTree();

        //Основные свойства
        public string Name { get; private set; }
        public int Rank => data.Splitters.Length - 1;
        public string[] Splitters => data.Splitters;
        public int Count => data.CountWords();
        //Размер буфера для чтения текста
        public static readonly int TEXT_BUFFER_SIZE = 1 << 16;

        public Language(string _name, string[] _splitters)
        {
            Name = _name;
            splitters = _splitters;
            parsers = splitters.Select(s => new Parser(s)).ToArray();
            data = new DataContainer(_name, splitters);
        }

        public Language(string _name)
        {
            Name = _name;
        }

        //--------------------------------------------------------------------------------------------
        //Методы работы с хранилищем данных
        //--------------------------------------------------------------------------------------------
        /// <summary>
        /// Подключение к хранилищу
        /// </summary>
        public void Connect()
        {
            if (data != null && data.IsConnected()) data.Disconnect();
            data = new DataContainer(Name);
            //Считаем разделители из БД
            data.Connect(Name);
            //Разделители Словаря 
            splitters = data.Splitters;
            //Создадим парсеры из разделителей
            parsers = splitters.Select(s => new Parser(s)).ToArray();
        }

        public void Disconnect()
        {
            data.Disconnect();
        }

        public bool IsConnected()
        {
            return data.IsConnected();
        }

        /// <summary>
        /// Создает базу данных лексикона с именем Name
        /// </summary>
        public void Create()
        {
            if (data.IsConnected()) data.Disconnect();
            if (File.Exists(Name)) File.Delete(Name);
            data = new DataContainer(Name, splitters);
            data.CreateDB();
        }

        /// <summary>
        /// Присваивает Name новое имя dbname и создает одноименную базу данных лексикона
        /// </summary>
        /// <param name="_dbname"></param>
        public void Create(string _dbname)
        {
            Name = _dbname;
            Create();
        }

        //--------------------------------------------------------------------------------------------
        //Работа с кэшем
        //--------------------------------------------------------------------------------------------
        protected void FreeMemory()
        {
            data.ClearCash();
        }

        //--------------------------------------------------------------------------------------------
        //Методы работы со словами
        //--------------------------------------------------------------------------------------------
        public Word Find(int i)
        {
            return data.Get(i);
        }

        /// <summary>
        /// Находит Слово по дочерним, если оно есть в хранилище  
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Word FindByChilds(int[] i)
        {
            return data.GetByChilds(i);
        }

        /// <summary>
        /// Преобразует текстовую строку в Терм ранга rank
        /// </summary>
        /// <param name="text"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        public Term ToTerm(string text, int rank)
        {
            text = parsers[rank].Normilize(text);
            return new Term(rank, 0, 0, text,
                rank == 0 ? null :
                parsers[rank - 1].
                Split(text).
                Where(s => !string.IsNullOrWhiteSpace(s)).
                Select(s => ToTerm(s, rank - 1)));
        }

        //public Term ToTerm(Word w, float _confidence = 1)
        //{
        //    return this.data.ToTerm(w, _confidence);
        //}

        public Term ToTerm(int i, float _confidence = 1)
        {
            return data.ToTerm(i, _confidence);
        }

        //--------------------------------------------------------------------------------------------
        //Методы обработки и анализа текста
        //--------------------------------------------------------------------------------------------
        ///Ищет в Словаре Слово ранга rank, соответствующее линейному тексту text и возвращает 
        ///идентификатор найденного слова. При отсутствии слова в словаре, и addIfNotExists=true
        ///добавляет слово в Словарь, иначе возвращает 0.
        private IEnumerable<int> Parse(string text, int rank, bool addIfNotExists = true)
        {
            text = parsers[rank].Normilize(text);
            IEnumerable<string> strings = parsers[rank].Split(text).Where(s => !string.IsNullOrEmpty(s));
            //Для слов ранга > 0 добавляем слова, которых еще нет
            List<int> result = strings.Select(s =>
            {
                int id;
                int[] childs = null;
                if (rank > 0)
                {
                    //получаем id дочерних слов ранга rank-1
                    childs = Parse(s, rank - 1).ToArray();
                    //Разбиение текста на слова ранга rank-1 не дал результата (нет подслов), 
                    //значит слово не найдено и не может быть создано - возвращаем 0
                    if (childs.Length == 0) return 0;
                    //Пытаемся найти в Словаре Слово по дочерним
                    id = data.GetIdByChilds(childs);
                    if (id == 0)
                        if (addIfNotExists)
                            id = data.Add(new Word(0, rank, "", childs, new int[0]));
                }
                else
                {
                    //Ищем в Словаре Слово ранга 0 в символьном представлении =s
                    id = data.GetId(s);
                    if (id == 0)
                        if (addIfNotExists)
                            id = data.Add(new Word(0, rank, s, null, new int[0]));
                }
                return id;
            }).Where(i => i != 0).ToList();
            return result;
        }

        /// <summary>
        /// Метод осуществляет идентификацию Терма по тексту term.text.
        /// Вычисляет значение confidence и id для терма term. Меняет переданный term, проставляя confidence и id
        /// </summary>
        /// <param name="term">изменяемый терм</param>
        /// <returns>возвращает ссылку на term (возврат значения для удобства использования в LINQ)</returns>
        public Term Identify(Term term)
        {
            //Для данного терма ранее могла быть проведена идентификация
            if (term.Identified) return term;
            if (term.rank == 0)
            {
                //При нулевом ранге терма (т.е. терм - это буква), confidence считаем исходя из наличия соответствующей буквы в алфавите
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
                int[] childs = term.Childs.
                    Distinct(new TermComparer()).
                    Select(c => Identify(c)).
                    Where(c => c.id != 0).
                    Select(c => c.id).
                    ToArray();
                sw.Stop();  //!!!
                Debug.WriteLine($"Identify->{term.ToString()}.childs [{childs.Length}]: {sw.Elapsed.TotalSeconds}");
                sw.Restart(); //!!!
                List<int> parents = data.GetParentsId(childs).ToList();
                sw.Stop();  //!!!
                Debug.WriteLine($"Identify->{term.ToString()}.parents [{parents.Count}]: {sw.Elapsed.TotalSeconds}");
                sw.Restart(); //!!!
                List<Term> context = parents.Select(p => ToTerm(p)).ToList();
                sw.Stop();  //!!!
                Debug.WriteLine($"Identify->{term.ToString()}.context [{context.Count}]: {sw.Elapsed.TotalSeconds}");
                //Поиск ближайшего родителя, т.е. родителя с максимумом сonfidence
                Pointer max = context.AsParallel().Aggregate(
                    new Pointer(),
                    (subtotal, thread_term) =>
                    {
                        float confidence = Confidence.Compare(term, thread_term);
                        if (subtotal.value < confidence) return new Pointer(thread_term.id, 0, confidence);
                        return subtotal;
                    },
                    (total, subtotal) => total.value < subtotal.value ? subtotal : total,
                    (final) => final);
                term.id = max.id;
                term.confidence = max.value;
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
            text = parsers[rank].Normilize(text);
            Term term = ToTerm(text, rank);
            return Identify(term);
        }

        /// <summary>
        /// Метод ищет схожие с текстом text слова в лексиконе и возвращает их представление в виде списка Термов
        /// </summary>
        /// <param name="text"></param>
        /// <param name="rank"></param>
        /// <param name="count">количество термов для возвращения. 0 - все </param>
        /// <returns></returns>
        public List<Term> Similars(string text, int rank = 2, int count = 0)
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
            int[] childs = term.Childs.
                Where(c => c.id != 0).
                Select(c => c.id).
                Distinct().
                ToArray();
            sw.Stop();  //!!!
            Debug.WriteLine($"Similars->childs [{childs.Length}]: {sw.Elapsed.TotalSeconds}");
            sw.Restart(); //!!!
            List<Term> context = data.
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
            if (count == 0) return context.ToList();
            else
            if (count > 0) return context.Take(count).ToList();
            else
                throw new ArgumentException("Количество возращаемых значений не может быть отрицательным");
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
            IEnumerable<Term> similars = Similars(text, rank)
                .Where(t => t.confidence >= similars_min_confidence)
                .Take(similars_max_count);
            sw.Stop();
            Debug.WriteLine($"Определение similars [{similars.Count()}]: {sw.Elapsed.TotalSeconds}");
            similars.ToList().ForEach(s => Debug.WriteLine($" [{s.confidence.ToString("F4")}] {s}"));    //!!!
            //Если похожих слов не нашли, возвращаем пустой список
            if (similars.Count() == 0) return result;
            //Запоминаем веса в привязке к словам
            Dictionary<int, float> weights = similars.ToDictionary(s => s.id, s => s.confidence);
            sw.Restart(); //!!!
            //Получаем контекст similars: все дочерние Слова для Слов, являющихся родителями similars
            Dictionary<int, Pointer> context = data.
                GetParentsWithChilds(weights.Keys.ToArray()).
                SelectMany(p => data.GetGrandchildsId(p.Item2.id).
                Select(gc => new Pointer(gc, 0, weights[p.Item1]))).
                Distinct(new PointerComparer()).
                ToDictionary(link => link.id, link => link);
            sw.Stop();  //!!!
            Debug.WriteLine($"Определение context [{context.Count}]: {sw.Elapsed.TotalSeconds}");
            //Запоминаем словарь допустимых слов, для использования в поиске
            sw.Restart(); //!!!
            context.Add(grammar.Root.id, new Pointer(0, 0, 0));
            Tuple<float, Stack<Pointer>> path = FindSequence(grammar.Root, context);
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
        private Tuple<float, Stack<Pointer>> FindSequence(Rule rule, Dictionary<int, Pointer> context)
        {
            if (!context.ContainsKey(rule.id)) return null;
            float head_weight = context[rule.id].value;
            float tail_weight = 0;
            Stack<Pointer> path = new Stack<Pointer>();
            Pointer found = new Pointer();
            foreach (Rule t in rule.Rules)
            {
                //if (!bag.ContainsKey(t.Key)) continue;
                Tuple<float, Stack<Pointer>> cur_path = FindSequence(t, context);
                if (cur_path == null) continue;
                if (tail_weight < cur_path.Item1)
                {
                    //критерий определяется как максимум сумм произведений вероятности слова в цепочке на confidence слова
                    found = new Pointer(t.id, 0, context[t.id].value /* rule.Confidence(t.id)*/);
                    tail_weight = cur_path.Item1;
                    path = cur_path.Item2;
                }
            }
            path.Push(new Pointer(rule.id, 0, head_weight));
            return new Tuple<float, Stack<Pointer>>((head_weight + tail_weight) / (path.Count / attenuation), path);
        }

        /// <summary>
        /// Метод выделяет "ключевые" подслова из Слова. При этом Слово - родительское для слова полученного
        /// из text. Если Слово - статья, то метод возвращает несколько предложений - "выжимку" из этой статьи.
        /// Алгоритм: 1) найти слова похожие на text; 2) выбрать из них "самое похожее"; 
        /// 3) получить родительское к Слову; 4) забрать дочерние к родительскому - то есть "братские" к Слову;
        /// 5) подсчитать матрицу растояний "братских" слов; 6) найти минимальный набор "братских" слов с минимальным 
        /// синтаксическими расстояниям до остальных "братских" слов.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        public IEnumerable<Term> GetCore(string text, int rank = 2)
        {
            //1. Найти Parent
            List<Term> similars = Similars(text, rank, similars_max_count);
            Dictionary<Term, double> parents = new Dictionary<Term, double>();
            //Для каждого parent вычисляем оценку как сумму confidence его потомков из similars
            similars
                .SelectMany(t => data
                    .GetParents(t.id))          //получаем Слова-родители t
                    .Select(p => data.ToTerm(p))  //преобразуем в термы
                .ToList()                       //ForEach есть IList, но нет в IEnumerable
                .ForEach(t =>
                {
                    if (!parents.ContainsKey(t)) parents[t] = 0;
                    parents[t] += t.confidence;
                });
            //Сортируем список пар по убыванию оценки и берем первый элемент
            IOrderedEnumerable<KeyValuePair<Term, double>> best_parents = parents.OrderByDescending(kvp => kvp.Value);
            Term best_parent = best_parents.First().Key;
            List<Term> terms = best_parent.Childs.Distinct(new TermComparer()).ToList();

            //2. Вычислить матрицу расстояний для дочерних Слов Parent
            Dictionary<string, double> dmatrix = SDMatrix(terms);

            //3. Найти минимальный набор дочерних Слов, покрывающих Parent на половину радиуса
            HashSet<Term> core = new HashSet<Term>();
            Dictionary<int, Term> B = terms.ToDictionary(t => t.id, t => t);
            double max_distance = MaxDistance(core, B.Values, dmatrix);
            double d = max_distance;
            //Вычисления проводятся либо пока не опустеет список B, либо пока макс. расстояние
            //между множествами A и B не будет меньше maxdist_divisor части от макс. расстояния 
            //между любыми двумя термами terms
            while (B.Count > 0 && core.Count < description_max_words)
            {
                //Найдем терм с минимальной суммой расстояний до остальных в B
                int id = MinSumDistance(B, dmatrix);
                //Перекладываем терм id из A в B
                core.Add(B[id]);
                B.Remove(id);
            }
            return core;
        }

        /// <summary>
        /// Возвращает максимальное расстояние между элементами двух множеств термов A и B
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns>максимальное расстояние между множествами термов A и B</returns>
        private double MaxDistance(IEnumerable<Term> A, IEnumerable<Term> B, Dictionary<string, double> dmatrix)
        {
            if (B.Count() == 0) return 0;
            if (A.Count() == 0) return double.MaxValue;
            double max = 0;
            foreach (Term a in A)
                foreach (Term b in B)
                {
                    double dist = dmatrix[$"{a.id}-{b.id}"];
                    if (dist > max)
                    {
                        max = dist;
                    }
                }
            return max;
        }

        /// <summary>
        /// Метод возвращает id терма с минимальной суммой расстояний от
        /// терма id до всех остальных термов из terms
        /// </summary>
        /// <param name="terms">набор термов среди которых ищется оптимальный</param>
        /// <param name="dmatrix">заранее вычисленная матрица расстояний</param>
        /// <returns></returns>
        private int MinSumDistance(Dictionary<int, Term> terms, Dictionary<string, double> dmatrix)
        {
            double min = double.MaxValue;
            int id = 0;
            //Ищем строку с минимальной суммой значений dmatrix по всем столбцам
            foreach (KeyValuePair<int, Term> r in terms)
            {
                double s = 0;
                foreach (KeyValuePair<int, Term> c in terms)
                {
                    s += dmatrix[$"{r.Key}-{c.Key}"];
                }
                if (min > s)
                {
                    id = r.Key;
                    min = s;
                }
            }
            return id;
        }

        /// <summary>
        /// Возвращает матрицу синтаксических расстояний для списка термов terms (Sintactic Distances Matrix).
        /// Синтаксическое расстояние считается как результат численного сравнения двух термов А и Б при помощи 
        /// функции Confidence.Compare(А,Б) 
        /// </summary>
        /// <param name="terms">список термов, для которых считаются взаимные расстояния</param>
        /// <returns>Матрица в виде словаря, ключ состоит из id терма-строки и id терма-столбца</returns>
        private Dictionary<string, double> SDMatrix(IList<Term> terms)
        {
            int n = terms.Count;
            Dictionary<string, double> d = new Dictionary<string, double>(n * n);
            for (int r = 0; r < terms.Count; r++)
                for (int c = 0; c < terms.Count; c++)
                    d[$"{terms[r].id}-{terms[c].id}"] = Confidence.Compare(terms[r], terms[c]);
            return d;
        }

        public Term NextNearest(string text, int rank = 2)
        {
            Stopwatch sw = new Stopwatch(); //!!!
            sw.Start(); //!!!
            //Ищем Слова похожие на text
            IEnumerable<Term> similars = Similars(text, rank)
                .Where(t => t.confidence >= similars_min_confidence)
                .Take(similars_max_count);
            sw.Stop();
            Debug.WriteLine($"Определение similars [{similars.Count()}]: {sw.Elapsed.TotalSeconds}");
            similars.ToList().ForEach(s => Debug.WriteLine($" [{s.confidence.ToString("F4")}] {s}"));    //!!!
            //Если похожих слов не нашли, возвращаем пустой список термов
            if (similars.Count() == 0) return null;
            //Запоминаем веса в привязке к похожим словам
            Dictionary<int, float> weights = similars.ToDictionary(s => s.id, s => s.confidence);
            sw.Restart(); //!!!
            //Получаем контекст Слова через similars: все дочерние Слова для Слов, являющихся родителями similars
            var context = data.
                GetParentsWithChilds(weights.Keys.ToArray()).
                Select(p => new Pointer(p.Item2.id, 0, weights[p.Item1])).
                Distinct(new PointerComparer());
            sw.Stop();  //!!!
            Debug.WriteLine($"Определение context [{context.Count()}]: {sw.Elapsed.TotalSeconds}");
            sw.Restart(); //!!!
            //Для каждого слова контекста id уверенностью c, вычисляем функцию f(c)=c/(1+min_dist(id)).
            //Чем меньше расстояние до слова, тем меньше делитель уверенности слова id
            var similar = similars.First();
            var min = data.dmatrix.Min(similar.id);
            var arrows = context
                .Select(c =>
                {
                    return new Tuple<int, int, float>(c.id, min.id, c.value / (1 + min.value));
                })                
                .ToList();
            arrows.Sort(new Comparison<Tuple<int, int, float>>((t1, t2) => Math.Sign(t2.Item3 - t1.Item3)));
            sw.Stop();  //!!!
            Debug.WriteLine($"Определение arrows [{arrows.Count()}]: {sw.Elapsed.TotalSeconds}");
            return ToTerm(arrows.First().Item2, arrows.First().Item3);
        }

        //--------------------------------------------------------------------------------------------
        //Методы построения лексикона, грамматики
        //--------------------------------------------------------------------------------------------
        public void Preprocessing(string filename, ProcessingType processingType)
        {
            switch (processingType)
            {
                case ProcessingType.Build: CreateLexicon(filename); break;
                case ProcessingType.Grammar: CreateGrammar(); break;
                case ProcessingType.Distance: CreatePDMatrix(); break;
                default: throw new NotImplementedException("Не реализованный тип обработки текста");
            }
        }

        /// <summary>
        /// Создание матрицы позиционных расстояний слов (Position Distances Matrix). 
        /// Позиционное расстояние от слова А до слова Б считается как среднее количество промежуточных слов в предложении
        /// между А и Б плюс один. Т.е. в предложении "Вася ушёл и не вернулся" расстояния равны:
        /// |Вася-ушёл|=1, |Вася-и|=2, |Вася-вернулся|=4.
        /// </summary>
        private void CreatePDMatrix()
        {
            //Матрица расстояний считается для слов одного ранга
            //Цикл по всем рангам
            for (int r = 1; r < Rank; r++)
            {
                var words = data.Where(w => w.rank == r);
                Console.WriteLine();
                ProgressInformer informer = new ProgressInformer($"Матрица расстояний слов ранга {r}:", words.Count())
                {
                    BarSize = 64,
                    UnitsOfMeasurment = "слов"
                };
                int i = 0;
                foreach (var w in words)
                {
                    CalcPositionDistances(w);
                    informer.Current = ++i;
                    if (i % 97 == 0) informer.Show();
                }
            };
        }

        private void CalcPositionDistances(Word w)
        {
            for (int i = 0; i < w.childs.Length - 1; i++)
                for (int j = i + 1; j < w.childs.Length; j++)
                    data.dmatrix.Add(w.childs[i], w.childs[j], j - i);
        }

        /// <summary>
        /// Составление Словаря по тексту из файла filename
        /// </summary>
        /// <param name="filename"></param>
        private void CreateLexicon(string filename)
        {
            if (!data.IsConnected())
                throw new Exception($"Нет подключения к базе данных!");
            char[] buffer = new char[Language.TEXT_BUFFER_SIZE];
            int count_chars = Language.TEXT_BUFFER_SIZE;
            using (StreamReader reader = File.OpenText(filename))
            {
                ProgressInformer informer = new ProgressInformer("Извлечение слов:", reader.BaseStream.Length)
                {
                    BarSize = 64,
                    UnitsOfMeasurment = "байт"
                };
                while (count_chars == TEXT_BUFFER_SIZE)
                {
                    count_chars = reader.ReadBlock(buffer, 0, Language.TEXT_BUFFER_SIZE);
                    string text = new string(buffer, 0, count_chars);
                    data.BeginTransaction();
                    IEnumerable<int> result = Parse(text, Rank, true);
                    data.EndTransaction();
                    informer.Current = reader.BaseStream.Position;
                    informer.Show();
                }
                //Очистка кэша
                data.ClearCash();
                Console.WriteLine($"\nСчитано {informer.Current} символов. Добавлено {data.CountWords()} слов.");
            }
        }

        /// <summary>
        /// Построение Грамматики (префиксного взвешенного дерева) по существующему Словарю
        /// </summary>
        private void CreateGrammar()
        {
            //grammar.Clear();
            IEnumerable<Word> words = data.Where(w => w.rank > 0);
            if (words.Count() == 0)
            {
                Console.WriteLine("Словарь не содержит слов. Сначала запустите обработку текста для составления Словаря.");
                return;
            }
            ProgressInformer informer = new ProgressInformer("Построение грамматики:", words.Count());
            int count = 0;
            foreach (Word w in words)
            {
                grammar.Add(w.childs);
                count++;
                informer.Current = count;
                if (count % (1 << 10) == 0) informer.Show();
                //очистка кэша на каждые 1<<20 слов
                if (count % (1 << 20) == 0) data.ClearCash();
            }
            informer.Set(count);
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
        /// <summary>
        /// Коэффициент затухания значимости слова. Нужен для ограничения количества слов в ответе - помогает уменьшать
        /// значимость найденного ответа при излишней многословности
        /// </summary>
        private const float attenuation = 100F;          //количество слов в ответе, которое обнуляет confidence ответа
        private const float similars_min_confidence = 0.5F;
        private const float followers_min_confidence = 0.02F;
        private const int followers_max_count = 16;
        private const double description_max_words = 16;
        //private int[] EmptyArray = new int[0];
    }
}
