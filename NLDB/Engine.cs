using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLDB.DAL;

namespace NLDB
{
    public enum OperationType
    {
        None,
        FileReading,
        FileWriting,
        TextNormalization,
        TextSplitting,
        WordsExtraction,
        DistancesCalculation,
        SimilarityCalculation
    };

    public enum ExecuteMode
    {
        Silent, Verbose, Debug
    };

    public class Engine
    {
        public ExecuteMode ExecuteMode { get; set; }

        public CalculationResult CalculationResult { get; private set; }

        public int Rank => DB.MaxRank;

        public object Data;

        public IEnumerable<DAL.Word> Words(int rank = 0, int count = 0)
        {
            string limit = count == 0 ? "" : $"LIMIT {count}";
            return DB.Words($"Rank={rank} {limit}");
        }

        public Engine(string dbpath)
        {
            this.dbpath = dbpath;
            DB = new DataBase(dbpath);
        }

        public void Create()
        {
            DB.Create();
        }

        public void Clear(string tableName = "")
        {
            if (tableName == "")
                DB.ClearAll();
            else
                try
                {
                    DB.Clear(tableName);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Ошибка очистки таблицы {tableName}:{e.Message}");
                }
        }

        public CalculationResult Execute(OperationType ptype, object parameter = null)
        {
            if (ExecuteMode == ExecuteMode.Verbose)
            {
                Console.WriteLine($"Начало операции {ptype}");
            }
            switch (ptype)
            {
                case OperationType.FileReading:
                    CalculationResult = ReadFile((string)parameter); break;
                case OperationType.FileWriting:
                    CalculationResult = WriteFile((string)parameter); break;
                case OperationType.TextNormalization:
                    CalculationResult = NormilizeText((string)parameter); break;
                case OperationType.TextSplitting:
                    CalculationResult = SplitText((string)parameter); break;
                case OperationType.WordsExtraction:
                    CalculationResult = ExtractWords((IEnumerable<string>)parameter, Rank); break;
                case OperationType.DistancesCalculation:
                    CalculationResult = CalculateDistances((IEnumerable<Word>)parameter); break;
                case OperationType.SimilarityCalculation:
                    CalculationResult = CalculateSimilarity((IList<Word>)parameter); break;
                default:
                    throw new NotImplementedException();
            }
            return CalculationResult;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        private CalculationResult CalculateSimilarity(IList<Word> words)
        {
            Stopwatch stopwatch = new Stopwatch();  //!!! отладочный таймер
            //Функция вычисляет попарные сходства между векторами-строками матрицы расстояний MatrixA и сохраняет результат в БД
            //Матрица симметричная, с нулевой главной диагональю
            int maxCount = words.Count();
            if (maxCount == 0) return new CalculationResult(this, OperationType.SimilarityCalculation, ResultType.Error);
            using (ProgressInformer informer = new ProgressInformer(prompt: $"Матрица подобия:", max: maxCount - 1, measurment: $"слов р[{words.First().Rank}]", barSize: 64))
            {
                //Вычисления производятся порциями по step строк. Выбирается диапазон величиной step индексов
                int step = 1 << 10;
                for (int j = 0; j <= maxCount / step; j++)
                {
                    DB.BeginTransaction();
                    stopwatch.Restart();    //!!!
                    int from = j * step;                            //левый индекс диапазона
                    int to = Math.Min(maxCount - 1, from + step);   //правый индекс диапазона
                    int size = to - from;
                    informer.Set(from);
                    CalculateSubmatrixB(words[from].Id, words[to].Id, words[from].Rank);
                    stopwatch.Stop();   //!!!
                    Debug.WriteLine($"Подматрица подобия ({size}x{size}): {stopwatch.Elapsed.TotalSeconds} сек.");  //!!!
                    DB.Commit();
                }
                informer.Set(maxCount - 1);
            }
            Data = null;
            return new CalculationResult(this, OperationType.SimilarityCalculation, ResultType.Success);
        }

        private void CalculateSubmatrixB(int from, int to, int rank)
        {
            //Функция вычисляет попарные расстояния между векторами-строками матрицы расстояний dmatrix и сохраняет результат в БД
            Dictionary<int, SparseRow<double>> rows = DB.GetARows(from, to, rank);
            List<List<BValue>> result = new List<List<BValue>>(rows.Count);
            //Сначала вычисления с сохранением в несколько списков параллельно
            rows.AsParallel().ForAll(row_a =>
            {
                List<BValue> result_row = new List<BValue>();
                foreach (KeyValuePair<int, SparseRow<double>> row_b in rows)
                {
                    double s = CosDistance(row_a.Value, row_b.Value);
                    if (s != 0)
                        result_row.Add(new BValue(rank, row_a.Key, row_b.Key, s));
                }
                if (result_row.Count > 0) result.Add(result_row);
            });
            if (result.Count == 0) return;
            //Запись в данных БД каждого из списков значений
            result.AsParallel().ForAll(row => { DB.InsertAll(row); });
        }

        private double CosDistance(SparseRow<double> row_a, SparseRow<double> row_b)
        {
            double m = 0, asize = 0, bsize = 0;
            foreach (int key_a in row_a.Keys)
            {
                double valueA = row_a[key_a];
                if (row_b.TryGetValue(key_a, out double valueB))
                {
                    asize += valueA * valueA;
                    bsize += valueB * valueB;
                    m += valueA * valueB;
                }
            }
            double divisor = Math.Sqrt(asize) * Math.Sqrt(bsize);
            if (divisor > 0)
                return m / divisor;
            else
                return 0;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        private CalculationResult CalculateDistances(IEnumerable<Word> words)
        {
            //Stopwatch sw = new Stopwatch();   //!!!
            //sw.Start(); //!!!
            using (ProgressInformer informer = new ProgressInformer($"Матрица расстояний:", words.Count(), measurment: "слов", barSize: 64))
            {
                int i = 0, divider = 123;
                DB.BeginTransaction();
                foreach (Word word in words)
                {
                    if (++i % divider == 0)
                    {
                        //sw.Stop();      //!!!
                        //Debug.WriteLine(sw.Elapsed.TotalSeconds);
                        //sw.Restart();   //!!!
                        informer.Set(i);
                    }
                    CalcPositionDistances(word);
                }
                informer.Set(i); // показать завершенный результат
                DB.Commit();
            }
            Data = null;
            return new CalculationResult(this, OperationType.DistancesCalculation, ResultType.Success);
        }

        private void CalcPositionDistances(Word w)
        {
            int[] childs = w.ChildsId;

            ConcurrentDictionary<int, List<AValue>> result = new ConcurrentDictionary<int, List<AValue>>(4, childs.Length - 1);
            Parallel.For(0, childs.Length, (i) =>
            {
                result[i] = new List<AValue>(childs.Length - i);
                for (int j = 0; j < childs.Length; j++)
                {
                    if (childs[i] != childs[j])
                    {
                        result[i].Add(new AValue(w.Rank - 1, childs[i], childs[j], j - i, 1));
                    }
                }
            });
            //Сброс данных в БД
            result.Values.ToList().ForEach(r => DB.InsertAll(r));
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        private CalculationResult ExtractWords(IEnumerable<string> strings, int rank)
        {
            DB.BeginTransaction();
            using (ProgressInformer informer = new ProgressInformer($"Слова ранга {rank}:", strings.Count(), "", 64))
            {
                Data = strings.SelectMany((s, i) =>
                {
                    informer.Set(i + 1);
                    return ExtractWordsFromString(s, rank);
                }).ToList();
            }
            DB.Commit();
            return new CalculationResult(this, OperationType.WordsExtraction, ResultType.Success, Data);
        }

        private int[] ExtractWordsFromString(string text, int rank)
        {
            IEnumerable<string> strings = DB.Split(text, rank).Where(s => !string.IsNullOrEmpty(s));
            return strings.Select(s =>
            {
                int id;
                int[] childsId = null;
                if (rank > 0)
                {
                    //получаем id дочерних слов ранга rank-1
                    childsId = ExtractWordsFromString(s, rank - 1);
                    if (childsId.Length == 0) return 0;
                    string childsString = string.Join(",", childsId);//.Aggregate("", (c, n) => c + (c != "" ? "," : "") + n.ToString());
                    //Пробуем найти Слово по дочерним элементам
                    Word word = DB.GetWordByChilds(childsString);
                    if (word == null)
                        id = DB.Add(new Word() { Rank = rank, Symbol = "", Childs = childsString });
                    else
                        id = word.Id;
                }
                else
                {
                    //Пробуем найти Слово по символу
                    Word word = DB.GetWordBySymbol(s);
                    if (word == null)
                        id = DB.Add(new Word() { Rank = rank, Symbol = s });
                    else
                        id = word.Id;
                }
                return id;
            })
            .Where(i => i != 0) //нули - не идентифицированные слова - пропускаем
            .ToArray();          //получим результат сразу
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        internal Term Recognize(Term term, int rank)
        {
            if (term.rank == 0)
            {
                //При нулевом ранге терма (т.е. терм - это буква), confidence считаем исходя из наличия соответствующей буквы в алфавите
                term.id = DB.GetWordBySymbol(term.text).Id;
                term.confidence = (term.id == 0 ? 0 : 1);
                term.Identified = true;
                return term;
            }
            else
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                //Если ранг терма больше нуля, то confidence считаем по набору дочерних элементов
                int[] childs = term
                    .Childs
                    .Distinct(new DAL.TermComparer())
                    .Select(c => Recognize(c, rank - 1))
                    .Where(c => c.id != 0)
                    .Select(c => c.id)
                    .ToArray();
                sw.Stop();  //!!!
                Debug.WriteLine($"Identify->{term.ToString()}.childs [{childs.Length}]: {sw.Elapsed.TotalSeconds}");
                sw.Restart(); //!!!
                List<DAL.Word> parents = DB.GetParents(childs).ToList();
                sw.Stop();  //!!!
                Debug.WriteLine($"Identify->{term.ToString()}.parents [{parents.Count}]: {sw.Elapsed.TotalSeconds}");
                sw.Restart(); //!!!
                List<Term> context = parents.Select(p => DB.ToTerm(p)).ToList();
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        internal List<Term> Similars(string text, int rank, int count)
        {
            if (count <= 0) throw new ArgumentException("Количество возращаемых значений должно быть положительным");
            text = Parser.Normilize(text);
            Stopwatch sw = new Stopwatch(); //!!!
            sw.Start(); //!!!
            Term term = DB.ToTerm(text, rank);
            sw.Stop();  //!!!
            Debug.WriteLine($"Similars->ToTerm: {sw.Elapsed.TotalSeconds}");
            sw.Restart(); //!!!
            Recognize(term, rank);
            sw.Stop();  //!!!
            Debug.WriteLine($"Similars->Identify: {sw.Elapsed.TotalSeconds}");
            //Для терма нулевого ранга возвращаем результат по наличию соответствующей буквы в алфавите
            if (term.rank == 0) return new List<Term> { term };
            //Определение контекста по дочерним словам
            sw.Restart(); //!!!
            int[] childs = term
                .Childs
                .Where(c => c.id != 0)
                .Select(c => c.id)
                .Distinct()
                .ToArray();
            sw.Stop();  //!!!
            Debug.WriteLine($"Similars->childs [{childs.Length}]: {sw.Elapsed.TotalSeconds}");
            sw.Restart(); //!!!
            List<Term> context = DB
                .GetParents(childs)
                .Distinct()
                .Select(p => DB.ToTerm(p)).
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
            return context.Take(count).ToList();
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        private CalculationResult ReadFile(string filename)
        {
            if (!File.Exists(filename))
                return new CalculationResult(this, OperationType.FileReading, ResultType.Error);
            using (StreamReader reader = File.OpenText(filename))
            {
                Data = reader.ReadToEnd();
            }
            return new CalculationResult(this, OperationType.FileReading, ResultType.Success);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        private CalculationResult WriteFile(string filename)
        {
            //TODO: продумать сохранение в файл разнух типов данных, хранимых по ссылке Data
            using (StreamWriter writer = File.CreateText(filename))
            {
                if (Data is List<int>)
                {
                    (Data as List<int>).ForEach(i => writer.Write(i + ";"));
                }
                else if (Data is string[])
                {
                    (Data as string[]).ToList().ForEach(i => writer.Write(i + ";"));
                }
                else
                    writer.Write(Data.ToString());
            }
            return new CalculationResult(this, OperationType.FileWriting, ResultType.Success);
        }

        private CalculationResult NormilizeText(string text)
        {
            Data = Parser.Normilize(text);
            return new CalculationResult(this, OperationType.TextNormalization, ResultType.Success);
        }

        private CalculationResult SplitText(string text)
        {
            Data = DB.Split(text);
            return new CalculationResult(this, OperationType.TextSplitting, ResultType.Success);
        }

        internal void Insert(Splitter splitter)
        {
            DB.Insert(splitter);
        }

        public Term ToTerm(Word word)
        {
            return DB.ToTerm(word);
        }

        //--------------------------------------------------------------------------------------------
        //Закрытые свойства
        //--------------------------------------------------------------------------------------------
        private DataBase DB { get; }
        private readonly string dbpath;
    }
}
