using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        public IEnumerable<Word> Words(int rank = 0, int count = 0)
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
                    //CalculationResult = CalculateSimilarity((IList<Word>)parameter); break;
                    CalculationResult = CalculateSimilarity2((int)parameter); break;
                default:
                    throw new NotImplementedException();
            }
            return CalculationResult;
        }

        private CalculationResult CalculateSimilarity2(int rank)
        {
            Stopwatch stopwatch = new Stopwatch();
            //Функция вычисляет попарные сходства между векторами-строками матрицы расстояний MatrixA и сохраняет результат в БД
            DB.BeginTransaction();
            stopwatch.Restart();
            Tuple<int, int> size = DB.GetAMatrixAsTuples(rank, out List<Tuple<int, int, double>> m);
            stopwatch.Stop();
            Debug.WriteLine($"Матрица {rank} считана {stopwatch.Elapsed.TotalSeconds} сек.");  //!!!
            stopwatch.Restart();
            MatrixDotSquare(m, rank);
            stopwatch.Stop();
            Debug.WriteLine($"Результат получен {stopwatch.Elapsed.TotalSeconds} сек.");  //!!!
            stopwatch.Restart();
            DB.Commit();
            stopwatch.Stop();
            Debug.WriteLine($"Записан в БД {stopwatch.Elapsed.TotalSeconds} сек.");  //!!!
            Data = null;
            return new CalculationResult(this, OperationType.SimilarityCalculation, ResultType.Success);
        }

        private long MatrixDotSquare(List<Tuple<int, int, double>> m, int rank)
        {
            //Control.UseNativeMKL();
            //Debug.WriteLine(Control.Describe());
            Debug.WriteLine($"Создание матрицы [{rank}] из {m.Count} ...");
            SparseMatrix M = new SparseMatrix(m);
            Debug.WriteLine($"Центрирование");
            M.CenterRows();
            Debug.WriteLine($"Нормализация");
            M.NormalizeRows();
            Debug.WriteLine($"Вычисление ковариации");
            var result = M.RowsCovariationMatrix();
            //Matrix<double> M = Matrix<double>.Build.SparseOfIndexed(size.Item1 + 1, size.Item2 + 1, m);
            //Matrix<double> N = Matrix<double>.Build.SparseOfIndexed(size.Item1 + 1, size.Item2 + 1, m);
            //var result = M.TransposeAndMultiply(N);
            Debug.WriteLine($"Вставка в БД...");
            DB.InsertAll(result.EnumerateIndexed(), rank);
            return 0;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Вычисление матрицы подобия
        /// </summary>
        /// <param name="words"></param>
        /// <returns></returns>
        private CalculationResult CalculateSimilarity(IList<Word> words)
        {
            Stopwatch stopwatch = new Stopwatch();  //!!! отладочный таймер
            //Функция вычисляет попарные сходства между векторами-строками матрицы расстояний MatrixA и сохраняет результат в БД
            //Матрица симметричная, с нулевой главной диагональю
            int maxCount = words.Count();
            long elementsCount = 0;
            int rank = words.First().Rank;
            if (maxCount == 0) return new CalculationResult(this, OperationType.SimilarityCalculation, ResultType.Error);
            int iterationsCount = maxCount * maxCount;
            using (ProgressInformer informer = new ProgressInformer(prompt: $"Матрица подобия:", max: maxCount, measurment: $"слов р{rank}", barSize: 64))
            {
                //Вычисления производятся порциями по step строк. Выбирается диапазон величиной step индексов
                int step = Math.Max(1, maxCount / STEPS_COUNT);
                for (int i = 0; i <= maxCount / step; i++)
                {
                    DB.BeginTransaction();
                    stopwatch.Restart();    //!!!
                    int rowsLeft = i * step;
                    informer.Set(rowsLeft);
                    MatrixDictionary rows = DB.GetARows(words.Skip(rowsLeft).Take(step).ToList(), rank);
                    for (int j = i; j <= maxCount / step; j++)
                    {
                        int columnsLeft = j * step;
                        MatrixDictionary columns = DB.GetARows(words.Skip(columnsLeft).Take(step).ToList(), rank);
                        elementsCount += CalculateSubmatrixB(rows, columns, rank);
                        Debug.WriteLine($"i={i}, j={j} [max={maxCount / step}], элементов = {elementsCount}");
                    }
                    DB.Commit();
                    stopwatch.Stop();   //!!!
                    Debug.WriteLine($"Подматрица подобия ({maxCount}x{maxCount}): {stopwatch.Elapsed.TotalSeconds} сек.");  //!!!
                }
                informer.Set(maxCount);
            }
            Data = null;
            return new CalculationResult(this, OperationType.SimilarityCalculation, ResultType.Success);
        }

        //Функция вычисления произведения векторов
        private long CalculateSubmatrixB(MatrixDictionary rows, MatrixDictionary columns, int rank)
        {
            int count = 0;
            if (rows.Count == 0 || columns.Count == 0) return 0;
            List<List<BValue>> result = new List<List<BValue>>(rows.Count * columns.Count);
            //Сначала вычисления с сохранением в несколько списков параллельно
            rows.AsParallel().ForAll(row =>
            //foreach (KeyValuePair<int, SparseVector> row in rows)
            {
                List<BValue> result_row = new List<BValue>();
                foreach (KeyValuePair<int, VectorDictionary> column in columns)
                {
                    double d;
                    if (row.Key == column.Key)
                        d = 1;  //Cos(0)=1
                    else
                        d = CosDistance(row.Value, column.Value);
                    //TODO: вероятно не имеет смысла записывать в БД значения подобия из интервала [-e;e], 0<e<0.9. Т.к. d в этом интервале означает несовместимость слов.
                    if (d != 0)
                    {
                        //Матрица симметричная
                        result_row.Add(new BValue(rank, row.Key, column.Key, d));
                        result_row.Add(new BValue(rank, column.Key, row.Key, d));
                        count += 2;
                    }
                }
                if (result_row.Count > 0) result.Add(result_row);
            });
            if (result.Count == 0) return 0;
            //Запись в данных БД каждого из списков значений
            result.AsParallel().ForAll(row => DB.InsertAll(row));
            return count;
        }

        private double CosDistance(VectorDictionary a, VectorDictionary b)
        {
            double m = 0, asize = 0, bsize = 0;
            foreach (int key_a in a.Keys)
            {
                double valueA = a[key_a];
                if (b.TryGetValue(key_a, out double valueB))
                {
                    //asize += valueA * valueA;
                    //bsize += valueB * valueB;
                    m += valueA * valueB;
                }
            }
            asize = a.Size;
            bsize = b.Size;
            double divisor = Math.Sqrt(asize) * Math.Sqrt(bsize);
            if (divisor > 0 && m > 0)
                return m / divisor;
            else
                return 0;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        private CalculationResult CalculateDistances(IEnumerable<Word> words)
        {
            //Stopwatch sw = new Stopwatch();   //!!!
            //sw.Start(); //!!!
            int maxCount = words.Count();
            if (maxCount == 0) return new CalculationResult(this, OperationType.DistancesCalculation, ResultType.Error);
            using (ProgressInformer informer = new ProgressInformer($"Матрица расстояний:", maxCount, measurment: $"слов р{words.First().Rank}", barSize: 64))
            {
                int step = Math.Max(1, maxCount / STEPS_COUNT);
                for (int j = 0; j <= maxCount / step; j++)
                {
                    DB.BeginTransaction();
                    int from = j * step;    //левый индекс диапазона
                    informer.Set(from + step);
                    IEnumerable<Word> wordsPacket = words.Skip(from).Take(step).ToList();
                    PositionsMean(wordsPacket);
                    DB.Commit();
                }
                informer.Set(maxCount);
            }
            Data = null;
            return new CalculationResult(this, OperationType.DistancesCalculation, ResultType.Success);
        }

        //Вычисляет матожидание вектора расстояний для каждого из слов words
        private void PositionsMean(IEnumerable<Word> words)
        {
            int wordsCount = words.Count();
            ConcurrentDictionary<int, Dictionary<int, AValue>> result = new ConcurrentDictionary<int, Dictionary<int, AValue>>(4, wordsCount);
            foreach (Word w in words)
            {
                int[] childs = w.ChildsId;
                Enumerable.Range(0, childs.Length - 1).AsParallel().ForAll((i) =>
                //for (int i = 0; i < childs.Length; i++)
                {
                    Dictionary<int, AValue> row = new Dictionary<int, AValue>(childs.Length - 1);
                    for (int j = 0; j < childs.Length; j++)
                    {
                        if (childs[i] == childs[j]) continue;
                        if (!row.TryGetValue(j, out AValue value))
                        {
                            row[j] = new AValue(w.Rank - 1, childs[i], childs[j], j - i, 1);
                        }
                        else
                        {
                            value.Count++;
                            value.Sum += j - i;
                            row[j] = value;
                        }
                    }
                    //Вставляем вектор-строку, только если он не пустой
                    if (row.Count > 0) result[i] = row;
                });
            }
            if (result.Count == 0) return;
            //Сброс данных в БД
            result.Values.ToList().ForEach(r => DB.InsertAll(r.Values));
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
        private const int STEPS_COUNT = 1 << 8;
    }
}
