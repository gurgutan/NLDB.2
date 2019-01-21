﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            Logger = new Logger(Path.ChangeExtension(dbpath, "log"));
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
                    {
                        int count = 0;
                        if (parameters.Length == 2) count = (int)parameters[1];
                        CalculationResult = ReadFile((string)parameters[0], count); break;
                    }
                case OperationType.FileWriting:
                    CalculationResult = WriteDataToFile((string)parameters[0]); break;
                case OperationType.TextNormalization:
                    CalculationResult = NormilizeText((string)parameters[0]); break;
                case OperationType.TextSplitting:
                    CalculationResult = SplitText((string)parameters[0]); break;
                case OperationType.WordsExtraction:
                    CalculationResult = ExtractWords((IEnumerable<string>)parameters[0], Rank); break;
                case OperationType.DistancesCalculation:
                    CalculationResult = CalculateDistances((IEnumerable<Word>)parameters[0]); break;
                case OperationType.SimilarityCalculation:
                    CalculationResult = CalculateSimilarity((int)parameters[0]); break;
                default:
                    throw new NotImplementedException();
            }
            Logger.WriteLine($"Завешена {CalculationResult.ToString()}");
            return CalculationResult;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------
        private CalculationResult CalculateSimilarity(int rank)
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
            int step = Math.Max(1, SIMILARITY_CALC_STEP);
            int maxNumber = maxCount / step;
            using (ProgressInformer informer = new ProgressInformer(prompt: $"Корреляция:", max: maxCount, measurment: $"слов {rank}", barSize: 64))
            {
                //Вычисления производятся порциями по step строк. Выбирается диапазон величиной step индексов
                Logger.WriteLine($"Параметры цикла: шаг={step}, количество={maxNumber}");
                for (int i = 0; i <= maxNumber; i++)
                {
                    int startRowNumber = i * step;
                    int endRowNumber = Math.Min(startRowNumber + step, words.Count - 1);
                    informer.Set(startRowNumber);
                    Logger.WriteLine($"Чтение матрицы из БД для {words[startRowNumber].Id}-{words[endRowNumber].Id}");
                    var aSize = DB.GetAMatrixAsTuples(rank, words[startRowNumber].Id, words[endRowNumber].Id, out List<Tuple<int, int, double>> rows);
                    SparseMatrix A = new SparseMatrix(rows);
                    for (int j = i; j <= maxNumber; j++)
                    {
                        DB.BeginTransaction();
                        stopwatch.Restart();
                        int startColumnNumber = j * step;
                        int endColumnNumber = Math.Min(startColumnNumber + step, words.Count - 1);
                        Logger.WriteLine($"Чтение подматрицы из БД для интервала Id {words[startColumnNumber].Id}-{words[endColumnNumber].Id}");
                        var bSize = DB.GetAMatrixAsTuples(rank, words[startColumnNumber].Id, words[endColumnNumber].Id, out List<Tuple<int, int, double>> columns);
                        Logger.WriteLine($"Построение разреженной подматрицы B");
                        SparseMatrix B = new SparseMatrix(columns);
                        Logger.WriteLine($"Начало вычислений A[{aSize.Item1}x{aSize.Item2}]*B[{bSize.Item1}x{bSize.Item2}]");
                        var count = MatrixDotSquare(A, B, rank);
                        elementsCount += count;
                        stopwatch.Stop();
                        Logger.WriteLine($"Шаг: [{i},{j}]/[{maxNumber}, {maxNumber}], эл-в:{count}, {stopwatch.Elapsed.TotalSeconds} сек");
                        DB.Commit();
                    }
                    Logger.WriteLine($"Подматрица подобия ({maxCount}x{maxCount}): {stopwatch.Elapsed.TotalSeconds} сек.");  //!!!
                }
                informer.Set(maxCount);
            }
            Data = null;
            return new CalculationResult(this, OperationType.SimilarityCalculation, ResultType.Success);
        }

        private int MatrixDotSquare(SparseMatrix A, SparseMatrix B, int rank)
        {
            Logger.WriteLine($"  1. Центрирование");
            A.CenterRows();
            A.NormalizeRows();
            B.CenterRows();
            B.NormalizeRows();
            Logger.WriteLine($"  2. Вычисление ковариации");
            SparseMatrix result = SparseMatrix.Covariation(A, B, zeroingRadius: 0.5);
            Logger.WriteLine($"  3. Формирование списка значений");
            IEnumerable<Tuple<int, int, double>> tuples = result.EnumerateIndexed();
            Logger.WriteLine($"  4. Запись в БД");
            return DB.InsertAll(tuples, rank);
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------
        private CalculationResult CalculateDistances(IEnumerable<Word> words)
        {
            //Stopwatch sw = new Stopwatch();   //!!!
            //sw.Start(); //!!!
            int maxCount = words.Count();
            if (maxCount == 0) return new CalculationResult(this, OperationType.DistancesCalculation, ResultType.Error);
            using (ProgressInformer informer = new ProgressInformer($"Матрица расстояний:", maxCount, measurment: $"слов р{words.First().Rank - 1}", barSize: 64))
            {
                int step = Math.Max(1, maxCount / DISTANCES_CALC_STEP);
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

        //---------------------------------------------------------------------------------------------------------------------------------------------------
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

        //---------------------------------------------------------------------------------------------------------------------------------------------------
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
                sw.Stop(); Debug.WriteLine($"Identify->{term.ToString()}.childs [{childs.Length}]: {sw.Elapsed.TotalSeconds}");
                sw.Restart(); //!!!
                List<Word> parents = DB.GetParents(childs).ToList();
                sw.Stop(); Debug.WriteLine($"Identify->{term.ToString()}.parents [{parents.Count}]: {sw.Elapsed.TotalSeconds}");
                sw.Restart(); //!!!
                List<Term> context = parents.Select(p => DB.ToTerm(p)).ToList();
                sw.Stop(); Debug.WriteLine($"Identify->{term.ToString()}.context [{context.Count}]: {sw.Elapsed.TotalSeconds}");
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

        //---------------------------------------------------------------------------------------------------------------------------------------------------
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
            sw.Restart(); //!!!
            //Определение контекста по дочерним словам
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

        public IEnumerable<Term> Nearest(Term term, int count = 1)
        {
            var result = new List<Term>();
            if (term.id == 0) return null;
            var size = DB.GetBMatrixAsTuples(term.rank, term.id, term.id + 1, out List<Tuple<int, int, double>> m);
            if (m.Count == 0) return result;
            var M = new SparseMatrix(m);
            var vector = M.First().V;
            if (vector.Count == 0) return result;
            return vector.OrderByDescending(iv => iv.V).Take(count).Select(iv => ToTerm(iv.Index));
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------
        private CalculationResult ReadFile(string filename, int length = 0)
        {
            if (!File.Exists(filename))
                return new CalculationResult(this, OperationType.FileReading, ResultType.Error);
            using (StreamReader reader = File.OpenText(filename))
            {
                if (length == 0) length = (int)reader.BaseStream.Length;
                char[] buffer = new char[length];
                int charCount = reader.Read(buffer, 0, length);
                Data = new String(buffer);
            }
            return new CalculationResult(this, OperationType.FileReading, ResultType.Success);
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------
        private CalculationResult WriteDataToFile(string filename)
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
        private readonly string dbpath;
        private const int SIMILARITY_CALC_STEP = 1 << 12;   //Оптимальный шаг для отношения Производительность/Память примерно 262 144
        private const int DISTANCES_CALC_STEP = 1 << 8;     //Оптимальный шаг для вычисления матрицы расстояний примерно 1024
    }
}
