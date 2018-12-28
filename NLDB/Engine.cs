using System;
using System.Collections.Generic;
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
        public DataBase DB { get => db; }
        private readonly string dbpath;
        private Parser[] parsers;
        private readonly OperationType CurrentOperationType = OperationType.None;

        public CalculationResult CalculationResult { get; private set; }
        public int Rank { get => parsers.Length - 1; }

        public object Data;

        public Engine(string dbpath)
        {
            this.dbpath = dbpath;
            db = new DataBase(dbpath);
        }

        public void Create()
        {
            db.Create();
            parsers = db.Table<Splitter>().OrderBy(r => r.Rank).Select(r => new Parser(r.Expr)).ToArray();
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
            var wordsIds = strings.Select<string, int>(s =>
            {
                int[] childsId = null;
                if (rank > 0)
                {
                    //получаем id дочерних слов ранга rank-1
                    childsId = (int[])ExtractWords(parsers[rank - 1].Split(s), rank - 1).Data;
                    if (childsId.Length == 0) return 0;
                    string childsString = childsId.Aggregate("", (c, n) => c + (c != "" ? "," : "") + n.ToString());
                    //Пробуем найти Слово по дочерним элементам
                    DAL.Word word = DB.GetWordByChilds(childsString);
                    if (word == null)
                        return DB.Add(new DAL.Word() { Rank = rank, Symbol = "", Childs = childsString });
                    return word.Id;
                }
                else
                {
                    //Пробуем найти Слово по символу
                    DAL.Word word = DB.GetWordBySymbol(s);
                    if (word == null)
                        return DB.Add(new DAL.Word() { Rank = rank, Symbol = s });
                    return word.Id;
                }
            }).Where(i => i != 0);
            return new CalculationResult(this, OperationType.WordsExtraction, ResultType.Success, wordsIds);
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

        //----------------------------------------------------------------------------------------------------------------------------------
        //private const int TEXT_BUFFER_SIZE = 1 << 18;

    }
}
