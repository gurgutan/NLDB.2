using System;
using System.IO;
using System.Linq;
using NLDB.DAL;

namespace NLDB
{
    internal class Program
    {

        private static void Main(string[] args)
        {
            string path = "/mnt/1C82D43582D414DC/Data/Result/5mb.db";

            string filename = "5mb.txt";
            string trainfile = @"D:\Data\Wiki\ru\" + filename;
            string dbpath = @"D:\Data\Result\" + Path.ChangeExtension(filename, "db");
            Engine engine = new Engine(path, ExecuteMode.Verbose);

            //engine.Create();
            //engine.Insert(new Splitter(0, ""));
            //engine.Insert(new Splitter(1, @"[^а-яё\d\{\}\-]+"));
            //engine.Insert(new Splitter(2, @"[\n\r\?\!\:\;]+"));
            //engine.Insert(new Splitter(3, @"\[\[{число}\]\]"));
            //engine
            //   .Execute(OperationType.FileReading, trainfile)
            //   .Then(OperationType.TextNormalization, engine.Data)
            //   .Then(OperationType.FileWriting, Path.ChangeExtension(dbpath, "norm"))
            //   .Then(OperationType.TextSplitting, engine.Data)
            //   .Then(OperationType.WordsExtraction, engine.Data);
            //engine.Clear("MatrixA");
            //engine.Execute(OperationType.DistancesCalculation, engine.Words(1));
            //engine.Execute(OperationType.DistancesCalculation, engine.Words(2));
            //engine.Execute(OperationType.DistancesCalculation, engine.Words(3));
            engine.Clear("MatrixB");
            engine.Execute(OperationType.SimilarityCalculation, 0, 0);
            engine.Execute(OperationType.SimilarityCalculation, 1, 0);
            engine.Execute(OperationType.SimilarityCalculation, 2, 0);
            //engine.Execute(OperationType.FileWriting, Path.ChangeExtension(dbpath, "words"));

            //Теперь будем использовать полученные данные
            Console.WriteLine("\n\nДля окончания диалога нажмите Enter");
            string line = "-";
            while (line != "")
            {
                Console.Write($"\n\nФраза: ");
                line = Console.ReadLine();
                if (line == "") continue;
                var terms = engine.Similars(line, 1, 8);
                Console.WriteLine(string.Join("\n", terms.Select(t => "[" + t.confidence.ToString("F4") + "] " + t.ToString())));
                var nearest = terms.SelectMany(t => engine.Nearest(terms.First(), 4)).Distinct().ToList();
                Console.WriteLine("\nСовместные:\n" + nearest.Aggregate("", (c, n) => c + $"\n" + "[" + n.confidence.ToString("F4") + "] " + n.ToString()));
            }
            Console.WriteLine("\n\nНажмите любую клавишу для продолжения");
            Console.ReadKey();
            //Отключаемся от хранилища
            //l.Disconnect();
        }

        //private static void TestLangConsole(Language l)
        //{
        //    int rank = 2;
        //    if (!l.IsConnected()) l.Connect();
        //    Queue<string> lines = new Queue<string>();
        //    int que_size = 1;
        //    string line = ">>";
        //    Console.WriteLine();
        //    while (line != "")
        //    {
        //        Console.WriteLine();
        //        Console.Write($"{rank}>>");
        //        line = Console.ReadLine();
        //        if (line == "") continue;
        //        lines.Enqueue(line);
        //        if (lines.Count > que_size) lines.Dequeue();
        //        string text = lines.Aggregate("", (c, n) => c == "" ? n : c + "\n" + n);
        //        Stopwatch sw = new Stopwatch();
        //        Console.WriteLine("\nПостроение цепочки");
        //        sw.Restart();
        //        IEnumerable<Term_old> core = l.GetCore(text, rank: 2);
        //        //List<Term> next = l.Next(text, rank);
        //        sw.Stop();
        //        Console.WriteLine(sw.Elapsed.TotalSeconds + " sec");
        //        if (core.Count() != 0)
        //            Console.WriteLine(core.Aggregate("", (c, n) => c + $"\n" + n.ToString()));
        //        //if (next.Count != 0)
        //        //    Console.WriteLine(next.Aggregate("", (c, n) => c + $" " + n.ToString()));
        //        //l.FreeMemory();
        //    }
        //    l.Disconnect();
        //}

        private static void NormilizeTest()
        {
            Parser parser = new Parser(@"[^а-яА-ЯёЁ0-9]");
            string result = Parser.Normilize("Привет ~~м^^ир/!");
            Console.WriteLine(result);
            Console.ReadKey();
        }

        public static void SplitTest()
        {
            Parser parser = new Parser(@"\[\[\d+\]\]");
            string[] result = parser.Split("[[324]] sdhjfjkshfjksdhf \n\n[[66]] uiqweuiqw diuhiqw \n\n [[454]] 273hd d7h sh d");
            foreach (string s in result)
                Console.Write(s + "\n------------------------------\n");
            Console.ReadKey();
        }

    }
}
