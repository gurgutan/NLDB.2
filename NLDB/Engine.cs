using System;
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
        WordsMeanCalculating,
        WordsSimilarityCalculating
    };

    public enum ExecuteMode
    {
        Silent, Verbose, Debug
    };

    public class Engine
    {
        public ExecuteMode ExecuteMode { get; set; }
        private readonly DataBase db;
        public DataBase DB => db;
        private readonly string dbpath;
        private Parser[] parsers;

        public CalculationResult CalculationResult { get; private set; }
        public int Rank => parsers.Length - 1;

        public object Data;

        public Engine(string dbpath)
        {
            this.dbpath = dbpath;
            db = new DataBase(dbpath);
        }

        public void Create()
        {
            db.Create();
            parsers = DB.Table<Splitter>().OrderBy(s => s.Rank).Select(r => new Parser(r.Expr)).ToArray();
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
                default:
                    throw new NotImplementedException();
            }
            return CalculationResult;
        }

        private CalculationResult ExtractWords(IEnumerable<string> strings, int rank)
        {
            DB.BeginTransaction();
            Data = strings.Select(s => ExtractWordsFromString(s, rank)).ToList();
            DB.Commit();
            return new CalculationResult(this, OperationType.WordsExtraction, ResultType.Success, Data);
        }

        private int[] ExtractWordsFromString(string text, int rank)
        {
            IEnumerable<string> strings = parsers[rank].Split(text).Where(s => !string.IsNullOrEmpty(s));
            int[] wordsIds = strings.Select<string, int>(s =>
            {
                int id;
                int[] childsId = null;
                if (rank > 0)
                {
                    //получаем id дочерних слов ранга rank-1
                    childsId = ExtractWordsFromString(s, rank - 1);
                    if (childsId.Length == 0) return 0;
                    string childsString = childsId.Aggregate("", (c, n) => c + (c != "" ? "," : "") + n.ToString());
                    //Пробуем найти Слово по дочерним элементам
                    DAL.Word word = DB.GetWordByChilds(childsString);
                    if (word == null)
                        id = DB.Add(new DAL.Word() { Rank = rank, Symbol = "", Childs = childsString });
                    else
                        id = word.Id;
                }
                else
                {
                    //Пробуем найти Слово по символу
                    DAL.Word word = DB.GetWordBySymbol(s);
                    if (word == null)
                    {
                        id = DB.Add(new DAL.Word() { Rank = rank, Symbol = s });
                        if (id % 101 == 0) Debug.WriteLine(word.Id);  //!!!
                    }
                    else
                        id = word.Id;
                }
                return id;
            })
            .Where(i => i != 0) //нули - не идентифицированные слова - пропускаем
            .ToArray();          //получим результат сразу
            return wordsIds;
        }

        internal List<Term> Recognize(string text, int count)
        {
            throw new NotImplementedException();
        }

        internal List<Term> Similars(string text, int count)
        {
            throw new NotImplementedException();
        }

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

        private CalculationResult WriteFile(string filename)
        {
            //TODO: продумать сохранение в файл разнух типов данных, хранимых по ссылке Data
            using (StreamWriter writer = File.CreateText(filename))
            {
                if (Data is IList<int>)
                {
                    (Data as IList<int>).ToList().ForEach(i => writer.Write(i + ";"));
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
            Data = parsers[Rank].Split(text);
            return new CalculationResult(this, OperationType.TextSplitting, ResultType.Success);
        }

        internal void Insert(Splitter splitter)
        {
            DB.Insert(splitter);
            parsers = DB.Table<Splitter>().OrderBy(s => s.Rank).Select(r => new Parser(r.Expr)).ToArray();
        }

        //----------------------------------------------------------------------------------------------------------------------------------
        //private const int TEXT_BUFFER_SIZE = 1 << 18;

    }
}
