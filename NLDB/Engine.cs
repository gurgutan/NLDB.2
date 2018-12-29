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

        public DataBase DB { get; }

        public CalculationResult CalculationResult { get; private set; }

        public int Rank => parsers.Length - 1;

        public object Data;

        public Engine(string dbpath)
        {
            this.dbpath = dbpath;
            DB = new DataBase(dbpath);
            parsers = DB.Splitters().Select(s => new Parser(s.Expression)).ToArray();
        }

        public void Clear() => DB.ClearAll();

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
            ProgressInformer informer = new ProgressInformer($"Слова ранга {rank}:", strings.Count(), "байт", 64);
            Data = strings.SelectMany((s, i) =>
            {
                informer.Set(i + 1);
                return ExtractWordsFromString(s, rank);
            }).ToList();
            DB.Commit();
            return new CalculationResult(this, OperationType.WordsExtraction, ResultType.Success, Data);
        }

        private int[] ExtractWordsFromString(string text, int rank)
        {
            IEnumerable<string> strings = parsers[rank].Split(text).Where(s => !string.IsNullOrEmpty(s));
            return strings.Select<string, int>(s =>
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
        }

        internal Term Recognize(string text, int rank)
        {
            Term term = ToTerm(text, rank);
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
                //Если ранг терма больше нуля, то confidence считаем по набору дочерних элементов
                var childs = term.Childs.Distinct().
                    Select(c => Identify(c)).
                    Where(c => c.id != 0).
                    Select(c => c.id).
                    ToArray();
                sw.Stop();  //!!!
                Debug.WriteLine($"Identify->{term.ToString()}.childs [{childs.Length}]: {sw.Elapsed.TotalSeconds}");
                sw.Restart(); //!!!
                List<int> parents = data.GetWordsParentsId(childs).ToList();
                sw.Stop();  //!!!
                Debug.WriteLine($"Identify->{term.ToString()}.parents [{parents.Count}]: {sw.Elapsed.TotalSeconds}");
                sw.Restart(); //!!!
                List<Term_old> context = parents.Select(p => ToTerm(p)).ToList();
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

        internal List<Term_old> Similars(string text, int count)
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
            Data = parsers[Rank].Split(text);
            return new CalculationResult(this, OperationType.TextSplitting, ResultType.Success);
        }

        internal void Insert(Splitter splitter)
        {
            DB.Insert(splitter);
            parsers = DB.Splitters().Select(s => new Parser(s.Expression)).ToArray();
        }

        //--------------------------------------------------------------------------------------------
        //Преобразование Слова в Терм
        //--------------------------------------------------------------------------------------------
        public DAL.Term ToTerm(DAL.Word w, float confidence = 1)
        {
            //if (this.terms.TryGetValue(w.Id, out Term t)) return t;
            if (w == null) return null;
            DAL.Term t = new DAL.Term(
                w.Rank,
                w.Id,
                _confidence: confidence,
                _text: w.Symbol,
                _childs: w.Rank == 0 ? null : w.ChildsId.Select(c => ToTerm(DB.GetWord(c))));
            //Сохраняем в кэш
            //terms[w.Id] = t;
            return t;
        }

        public DAL.Term ToTerm(string text, int rank)
        {
            text = Parser.Normilize(text);
            return new DAL.Term(rank, 0, 0, text,
                rank == 0 ? null :
                parsers[rank - 1].
                Split(text).
                Where(s => !string.IsNullOrWhiteSpace(s)).
                Select(s => ToTerm(s, rank - 1)));
        }

        //--------------------------------------------------------------------------------------------
        //Закрытые свойства
        //--------------------------------------------------------------------------------------------
        private readonly string dbpath;
        private Parser[] parsers;
    }
}
