using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using NLDB.DAL;
using NLDB.Utils;

namespace NLDB
{
    public class Engine : IDisposable
    {
        public readonly Logger Logger;

        public ExecuteMode ExecuteMode { get; set; }

        public CalculationResult CalculationResult { get; private set; }

        public int Rank => DB.MaxRank;

        //TODO: Потом можно сделать настраиваемый размер буфера для чтения текста
        public int TextBufferSize => TEXT_BUFFER_SIZE;

        public object Data { get; private set; }

        public IEnumerable<Word> Words(int rank = 0, int count = 0)
        {
            string limit = count == 0 ? "" : $"LIMIT {count}";
            return DB.Words($"Rank={rank} {limit}");
        }

        public Engine(string dbpath, ExecuteMode mode = ExecuteMode.Verbose)
        {
            this.dbpath = dbpath;
            ExecuteMode = mode;
            DB = new DataBase(dbpath);
            var date = DateTime.Now;
            string logFileName = Path.ChangeExtension(dbpath, "." + string.Join("_", date.Day, date.Month, date.Year, date.Hour, date.Minute, date.Second + ".log"));
            Logger = new Logger(logFileName, false, true);
            Logger.WriteLine($"Подключено: '{dbpath}'");
        }

        public void Create()
        {
            DB.Create();
            Logger.WriteLine($"Создана БД '{dbpath}'");
        }

        public void Clear(string tableName = "")
        {
            if (tableName == "")
                DB.ClearAll();
            else
                try
                {
                    DB.Clear(tableName);
                    Logger.WriteLine($"Очищена таблица БД '{tableName}'");
                }
                catch (Exception e)
                {
                    Logger.WriteLine($"Ошибка очистки таблицы {tableName}:{e.Message}");
                }
        }

        public CalculationResult Execute(OperationType ptype, params object[] parameters)
        {
            Logger.WriteLine($"Операция {ptype}");
            switch (ptype)
            {
                case OperationType.FileReading:
                    CalculationResult = ReadFile((string)parameters[0], TEXT_BUFFER_SIZE); break;
                case OperationType.FileWriting:
                    CalculationResult = WriteDataToFile((string)parameters[0]); break;
                case OperationType.TextNormalization:
                    CalculationResult = NormilizeText((IEnumerable<string>)parameters[0]); break;
                case OperationType.TextSplitting:
                    CalculationResult = SplitText((IEnumerable<string>)parameters[0]); break;
                case OperationType.WordsExtraction:
                    CalculationResult = ExtractWords((IEnumerable<string>)parameters[0], Rank); break;
                case OperationType.DistancesCalculation:
                    CalculationResult = CalculateDistances((IEnumerable<Word>)parameters[0]); break;
                case OperationType.SimilarityCalculation:
                    CalculationResult = CalculateSimilarity((int)parameters[0]/*rank*/, (int)parameters[1]/*start from word*/); break;
                case OperationType.GrammarCreating:
                    CalculationResult = BuidGrammar((int)parameters[0]/*rank*/); break;
                default:
                    throw new NotImplementedException();
            }
            Logger.WriteLine($"Завешена {CalculationResult.ToString()}");
            return CalculationResult;
        }

        /// <summary>
        /// Построение грамматики для слов ранга rank
        /// </summary>
        /// <param name="rank"></param>
        /// <returns></returns>
        private CalculationResult BuidGrammar(int rank)
        {
            if (rank + 1 > DB.MaxRank)
                throw new ArgumentOutOfRangeException($"Максимально допустимый ранг слов:{DB.MaxRank - 1}");
            // перебор идет дочерних слов, поэтому получаем список слов ранга rank+1
            List<Word> words = Words(rank + 1).ToList();
            words.ForEach(w => grammar.Add(w.ChildsId));
            return new CalculationResult(this, OperationType.GrammarCreating, ResultType.Success, grammar);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        private CalculationResult CalculateSimilarity(int rank, int continueFrom = 0)
        {
            Stopwatch stopwatch = new Stopwatch();
            //Функция вычисляет попарные сходства между векторами-строками матрицы расстояний MatrixA и сохраняет результат в БД
            List<Word> words = Words(rank).ToList();
            Logger.WriteLine($"Вычисление матрицы корреляции для {words.Count} слов ранга {rank}");
            int maxCount = words.Count();
            long elementsCount = 0;
            if (maxCount == 0)
            {
                Logger.WriteLine($"Отстутсвуют слова для расчета корреляции");
                return new CalculationResult(this, OperationType.SimilarityCalculation, ResultType.Error);
            }
            int maxIndex = words.Last().Id;
            Control.UseBestProviders();
            Control.UseMultiThreading();
            //Console.WriteLine(MathNet.Numerics.Control.Describe());

            int step = Math.Max(1, SIMILARITY_CALC_STEP);
            int max_number = maxCount / step;
            Logger.WriteLine($"Параметры: шаг={step}, количество={max_number}");
            using (ProgressInformer informer = new ProgressInformer(prompt: $"Корреляция:", max: max_number * max_number, measurment: $"слов {rank}", barSize: 64, fps: 10))
            {
                informer.Set(0, true);
                //Вычисления производятся порциями по step строк. Выбирается диапазон величиной step индексов
                for (int i = continueFrom; i <= max_number; i++)
                {
                    int startRowNumber = i * step;
                    int endRowNumber = Math.Min(startRowNumber + step, words.Count - 1);
                    Logger.WriteLine($"Чтение матрицы для строк {words[startRowNumber].Id}-{words[endRowNumber].Id}");
                    var asize = DB.GetAMatrixAsTuples(rank, words[startRowNumber].Id, words[endRowNumber].Id, out var rows);
                    if (asize.Item1 == 0 || asize.Item2 == 0) continue;
                    //SparseMatrix A = new SparseMatrix(rows);
                    var A = Matrix<double>.Build.SparseOfIndexed(asize.Item1, maxIndex, rows);
                    for (int j = i; j <= max_number; j++)
                    {
                        DB.BeginTransaction();
                        stopwatch.Restart();
                        int startColumnNumber = j * step;
                        int endColumnNumber = Math.Min(startColumnNumber + step, words.Count - 1);
                        informer.Set(i * max_number + j, false);
                        Logger.WriteLine($"Чтение матрицы для строк {words[startColumnNumber].Id}-{words[endColumnNumber].Id}");
                        var bsize = DB.GetAMatrixAsTuples(rank, words[startColumnNumber].Id, words[endColumnNumber].Id, out var columns);
                        if (bsize.Item1 == 0 || bsize.Item2 == 0) continue;
                        //Logger.WriteLine($"Построение разреженной подматрицы B");
                        //SparseMatrix B = new SparseMatrix(columns);
                        var B = Matrix<double>.Build.SparseOfIndexed(bsize.Item1, maxIndex, columns);
                        //elementsCount += MatrixDotSquare(A, B, rank);
                        elementsCount += CovariationMKL(A, B, rank);
                        stopwatch.Stop();
                        Logger.WriteLine($"[{i}/{max_number},{j}/{max_number}], всего эл-в:{elementsCount}, {stopwatch.Elapsed.TotalSeconds} сек");
                        DB.Commit();
                    }
                    Logger.WriteLine($"Подматрица подобия ({maxCount}x{maxCount}): {stopwatch.Elapsed.TotalSeconds} сек.");  //!!!
                }
                informer.Set(max_number * max_number, true);
            }
            Data = null;
            return new CalculationResult(this, OperationType.SimilarityCalculation, ResultType.Success);
        }

        private int MatrixDotSquare(SparseMatrix A, SparseMatrix B, int rank)
        {
            //Logger.WriteLine($"  1. Центрирование");
            //A.CenterRows();
            A.NormalizeRows();
            //B.CenterRows();
            B.NormalizeRows();
            Logger.WriteLine($"  2. Вычисление ковариации");
            SparseMatrix result = SparseMatrix.Covariation(A, B, 0.1);
            Logger.WriteLine($"  3. Формирование списка значений");
            List<Tuple<int, int, double>> tuples = result.EnumerateIndexed().ToList();
            Logger.WriteLine($"  4. Запись в БД");
            return DB.InsertAll(tuples, rank);
        }

        public int CovariationMKL(Matrix<double> a, Matrix<double> b, int rank)
        {
            Logger.WriteLine($"  1. Нормализация");
            a = a.NormalizeRows(2.0);
            b = b.NormalizeRows(2.0);
            Logger.WriteLine($"  2. Вычисление ковариации");
            var m = a.Multiply(b.Transpose());
            Logger.WriteLine($"  3. Формирование списка значений");
            var values = m.EnumerateIndexed(Zeros.AllowSkip).Where(v => Math.Abs(v.Item3) > 0.01);
            Logger.WriteLine($"  4. Запись в БД");
            return DB.InsertAll(values, rank);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        private CalculationResult CalculateDistances(IEnumerable<Word> words)
        {
            //Stopwatch sw = new Stopwatch();   //!!!
            //sw.Start(); //!!!
            int maxCount = words.Count();
            if (maxCount == 0) return new CalculationResult(this, OperationType.DistancesCalculation, ResultType.Error);
            using (ProgressInformer informer = new ProgressInformer($"Матрица расстояний:", maxCount, measurment: $"слов р{words.First().Rank - 1}", barSize: 64))
            {
                Stopwatch sw = new Stopwatch();
                int step = Math.Max(1, DISTANCES_CALC_STEP);
                for (int j = 0; j <= maxCount / step; j++)
                {
                    DB.BeginTransaction();
                    int from = j * step;    //левый индекс диапазона
                    int to = Math.Min(from + step, maxCount);
                    informer.Set(to);
                    IEnumerable<Word> wordsPacket = words.Skip(from).Take(step).ToList();
                    sw.Restart();
                    //PositionsMean(wordsPacket);
                    ContextMatrix(wordsPacket);
                    sw.Stop();
                    //Debug.WriteLine(sw.Elapsed.TotalSeconds);
                    DB.Commit();
                }
                informer.Set(maxCount);
            }
            Data = null;
            return new CalculationResult(this, OperationType.DistancesCalculation, ResultType.Success);
        }

        ////Вычисляет матожидание вектора расстояний для каждого из слов words
        //private int PositionsMean(IEnumerable<Word> words)
        //{
        //    int wordsCount = words.Count();
        //    ConcurrentDictionary<int, Dictionary<int, AValue>> result = new ConcurrentDictionary<int, Dictionary<int, AValue>>(4, wordsCount);
        //    foreach (var w in words)
        //    {
        //        int[] childs = w.ChildsId;
        //        Enumerable.Range(0, childs.Length - 1).AsParallel().ForAll((i) =>
        //        {
        //            Dictionary<int, AValue> row = new Dictionary<int, AValue>(childs.Length - 1);
        //            for (int j = 0; j < childs.Length; j++)
        //            {
        //                if (childs[i] == childs[j]) continue;
        //                if (!row.TryGetValue(j, out var value))
        //                {
        //                    row[j] = new AValue(w.Rank - 1, childs[i], childs[j], j - i, 1);
        //                }
        //                else
        //                {
        //                    value.Count++;
        //                    value.Sum += j - i;
        //                    row[j] = value;
        //                }
        //            }
        //            //Вставляем вектор-строку, только если он не пустой
        //            if (row.Count > 0) while (!result.TryAdd(i, row)) ;
        //        });
        //    }
        //    if (result.Count == 0) return 0;
        //    //Сброс данных в БД
        //    Debug.WriteLine(result.Count);
        //    List<ulong> c = result.Values.SelectMany(v => v.Values).Select(v => (ulong)v.R << 32 | (uint)v.C).OrderBy(v => v).ToList();
        //    result.Values.ToList().ForEach(r => DB.InsertAll(r.Values));
        //    return result.Count;
        //}

        private int ContextMatrix(IEnumerable<Word> words)
        {
            int rank = words.First().Rank;
            List<AValue> result = words
                .AsParallel()
                .SelectMany(w =>
                {
                    int[] childs = w.ChildsId;
                    return childs.SelectMany((a, i) => childs.Select((b, j) => new AValue(w.Rank - 1, a, b, j - i, 1)));

                })
                .GroupBy(v => v.Key)
                .AsParallel()
                .Select(group => new AValue(rank, AValue.RowFromKey(group.Key), AValue.ColumnFromKey(group.Key), group.AsParallel().Sum(e => e.Sum), group.Count()))
                .ToList();
            Debug.WriteLine(result.Count);
            //List<ulong> keys = result.Select(v => (ulong)v.R << 32 | (uint)v.C).Distinct().OrderBy(v => v).ToList();
            DB.InsertAll(result);
            return result.Count;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        private CalculationResult ExtractWords(IEnumerable<string> strings, int rank)
        {
            using (ProgressInformer informer = new ProgressInformer($"Слова ранга {rank}:", strings.Count(), "", 64))
            {
                Data = strings.SelectMany((s, i) =>
                {
                    DB.BeginTransaction();
                    informer.Set(i + 1);
                    int[] result = ExtractWordsFromString(s, rank);
                    DB.Commit();
                    return result;
                }).ToList();
            }
            return new CalculationResult(this, OperationType.WordsExtraction, ResultType.Success, Data);
        }

        private int[] ExtractWordsFromString(string text, int rank)
        {
            var strings = DB.Split(text, rank).Where(s => !string.IsNullOrEmpty(s));
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
                    var word = DB.GetWordByChilds(childsString);
                    if (word == null)
                        id = DB.Add(new Word() { Rank = rank, Symbol = "", Childs = childsString });
                    else
                        id = word.Id;
                }
                else
                {
                    //Пробуем найти Слово по символу
                    var word = DB.GetWordBySymbol(s);
                    if (word == null)
                        id = DB.Add(new Word() { Rank = rank, Symbol = s });
                    else
                        id = word.Id;
                }
                return id;
            })
            .Where(i => i != 0) //нули - не идентифицированные слова - пропускаем
            .ToArray();         //получим результат сразу
        }

        internal List<Term> Similars(Term term, int count = 1)
        {
            if (count <= 0) throw new ArgumentException("Количество возращаемых значений должно быть положительным");
            List<Term> result = new List<Term>();
            if (term.rank == 0)
            {
                //При нулевом ранге терма (т.е. терм - это буква), confidence считаем исходя из наличия соответствующей буквы в алфавите
                term.id = DB.GetWordBySymbol(term.text).Id;
                term.confidence = (term.id == 0 ? 0 : 1);
                term.Identified = true;
                result.Add(term);
            }
            else
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                //Если ранг терма больше нуля, то confidence считаем по набору дочерних элементов
                int[] childs = term
                    .Childs
                    .Distinct(new DAL.TermComparer())
                    .SelectMany(c => Similars(term: c, count: 1))
                    .Where(c => c.id != 0)
                    .Select(c => c.id)
                    .ToArray();
                sw.Stop(); Debug.WriteLine($"Similars->{term.ToString()}.childs [{childs.Length}]: {sw.Elapsed.TotalSeconds}");
                sw.Restart(); //!!!
                List<Word> parents = DB.GetParents(childs).Distinct().ToList();
                sw.Stop(); Debug.WriteLine($"Similars->{term.ToString()}.parents [{parents.Count}]: {sw.Elapsed.TotalSeconds}");
                sw.Restart(); //!!!
                List<Term> context = parents.Select(p => DB.ToTerm(p)).ToList();
                sw.Stop(); Debug.WriteLine($"Similars->{term.ToString()}.context [{context.Count}]: {sw.Elapsed.TotalSeconds}");
                //Поиск ближайшего родителя, т.е. родителя с максимумом сonfidence
                sw.Restart(); //!!!
                context.AsParallel().ForAll(p => p.confidence = Confidence.Compare(term, p));
                context.Sort(new Comparison<Term>((t1, t2) => Math.Sign(t2.confidence - t1.confidence)));
                result = context.Take(count).ToList();
            }
            return result;
        }

        internal List<Term> Similars(string text, int rank = 1, int count = 1)
        {
            if (count <= 0) throw new ArgumentException("Количество возращаемых значений должно быть положительным");
            List<Term> result = new List<Term>();
            Term term = DB.ToTerm(text, rank);
            return Similars(term, count);
        }

        public IEnumerable<Term> Nearest(Term term, int count = 1)
        {
            List<Term> result = new List<Term>();
            if (term.id == 0) return null;
            var size = DB.GetBMatrixAsTuples(term.rank, term.id, term.id + 1, out var m);
            if (m.Count == 0) return result;
            SparseMatrix M = new SparseMatrix(m);
            var vector = M.First().V;  //первая и единственная строка матрицы - вектор расстояний
            if (vector.Count == 0) return result;
            return vector.OrderByDescending(iv => iv.Value).Take(count).Select(iv => { var t = ToTerm(iv.Index); t.confidence = (float)iv.Value; return t; });
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Записывает в Data массив строк, считанных из файла filename. Каждая строка содержит не более blockSize символов
        /// </summary>
        /// <param name="filename">имя входного файла для чтения</param>
        /// <param name="blockSize">размер буфера для чтения - 1 Гбайт по умолчанию</param>
        /// <returns>Результат операции</returns>
        private CalculationResult ReadFile(string filename, int blockSize = 1 << 30)
        {
            if (!File.Exists(filename))
                return new CalculationResult(this, OperationType.FileReading, ResultType.Error);
            List<string> result = new List<string>();
            using (var reader = File.OpenText(filename))
            using (ProgressInformer informer = new ProgressInformer("Чтение файла:", reader.BaseStream.Length, "байт"))
            {
                //Считаем, что входной поток - UTF-8
                int tail = reader.BaseStream.Length % blockSize == 0 ? 0 : 1;
                char[] buffer = new char[blockSize];
                while (reader.Read(buffer, 0, blockSize) > 0)
                {
                    informer.Set(reader.BaseStream.Position);
                    result.Add(new string(buffer));
                }
                informer.Set(reader.BaseStream.Length);
            }
            Data = result;
            GC.Collect();
            return new CalculationResult(this, OperationType.FileReading, ResultType.Success);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------
        private CalculationResult WriteDataToFile(string filename)
        {
            if (Data == null) return new CalculationResult(this, OperationType.FileWriting, ResultType.Error);
            //TODO: продумать сохранение в файл разнух типов данных, хранимых по ссылке Data
            using (var writer = File.CreateText(filename))
            {
                if (Data is IList<int>)
                    (Data as IList<int>).ToList().ForEach(i => writer.Write(i + ";"));
                else if (Data is IList<string>)
                    (Data as IList<string>).ToList().ForEach(i => writer.Write(i + ";"));
                else
                    writer.Write(Data.ToString());
            }
            return new CalculationResult(this, OperationType.FileWriting, ResultType.Success);
        }

        private CalculationResult NormilizeText(IEnumerable<string> text)
        {
            List<string> result = new List<string>();
            using (ProgressInformer informer = new ProgressInformer("Нормализация:", text.Count(), "блоков"))
            {
                text.Select((s, i) =>
                {
                    informer.Set(i);
                    result.Add(Parser.Normilize(s));
                    return 0;
                }).ToList();
                informer.Set(text.Count());
            }
            Data = result;
            GC.Collect();
            return new CalculationResult(this, OperationType.TextNormalization, ResultType.Success);
        }

        private CalculationResult SplitText(IEnumerable<string> text)
        {
            List<string> result = new List<string>();
            using (ProgressInformer informer = new ProgressInformer("Разбиение на строки:", text.Count() - 1, "блоков"))
            {
                text.Select((s, i) =>
                {
                    informer.Set(i);
                    result.AddRange(DB.Split(s));
                    return 0;
                }).ToList();
            }
            Data = result;
            GC.Collect();
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

        public Term ToTerm(int id)
        {
            var word = DB.GetWord(id);
            if (word == null) return null;
            return DB.ToTerm(word);
        }

        public void Dispose()
        {
            Logger.WriteLine($"Закрытие подключения к '{dbpath}'");
            ((IDisposable)DB).Dispose();
            Logger.Dispose();
        }

        //--------------------------------------------------------------------------------------------
        //Закрытые свойства
        //--------------------------------------------------------------------------------------------
        private DataBase DB { get; }
        private Grammar grammar = new Grammar(2);
        private readonly string dbpath;
        private const int SIMILARITY_CALC_STEP = 1 << 12;   //Оптимальный шаг для отношения Производительность/Память примерно 262 144
        private const int DISTANCES_CALC_STEP = 1 << 20;     //Оптимальный шаг для вычисления матрицы расстояний примерно 1024
        private const int TEXT_BUFFER_SIZE = 1 << 28;
    }
}
