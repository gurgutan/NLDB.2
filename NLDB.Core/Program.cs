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
            //string path = "/mnt/1C82D43582D414DC/Data/Result/5mb.db";

            string filename = "884mb.txt";
            string trainfile = @"D:\Data\Wiki\ru\" + filename;
            string dbpath = @"D:\Data\Result\CS\" + Path.ChangeExtension(filename, "db");
            Engine engine = new Engine(dbpath, ExecuteMode.Verbose);

            engine.Create();
            engine.Insert(new Splitter(0, ""));
            engine.Insert(new Splitter(1, @"[^а-яё\{\}\-\p{Pd}]+"));
            engine.Insert(new Splitter(2, @"[\n\r\?\!\:\;\p{Pd}]+"));
            engine.Insert(new Splitter(3, @"\[\[{ХХХ}\]\]"));
            engine
               .Execute(OperationType.FileReading, trainfile)
               .Then(OperationType.TextNormalization, engine.Data)
               .Then(OperationType.FileWriting, dbpath.Replace(".db", ".norm"))
               .Then(OperationType.TextSplitting, engine.Data)
               .Then(OperationType.WordsExtraction, engine.Data);

            //// Вычисления метрик
            //engine.Clear("MatrixA");
            //engine.Execute(OperationType.DistancesCalculation, engine.Words(1));
            //engine.Execute(OperationType.DistancesCalculation, engine.Words(2));
            //engine.Execute(OperationType.DistancesCalculation, engine.Words(3));
            //engine.Clear("MatrixB");
            //engine.Execute(OperationType.SimilarityCalculation, 0, 0);
            //engine.Execute(OperationType.SimilarityCalculation, 1, 0);
            //engine.Execute(OperationType.SimilarityCalculation, 2, 0);

            Console.WriteLine($"Создание грамматики");
            engine.Execute(OperationType.GrammarCreating);
            Console.WriteLine($"Загрузка грамматики из БД");
            var result = engine.Execute(OperationType.GrammarLoading);
            Grammar grammar = (result.Data as Grammar);

            Console.WriteLine($"Количество элементов грамматики: {grammar.NodesCount}");
            Console.WriteLine($"Количество связей грамматики: {grammar.LinksCount}");

            //Теперь будем использовать полученные данные
            Console.WriteLine("\n\nДля окончания диалога нажмите Enter");
            string line = "-";
            while (line != "")
            {
                Console.Write($"\n\nФраза: ");
                line = Console.ReadLine();
                if (line == "") continue;
                var terms = engine.Similars(line, -1, 8);
                if (terms.Count == 0) continue;
                Console.WriteLine(string.Join("\n", terms.Select(t => "[" + t.confidence.ToString("F4") + "] " + t.ToString())));
                var nodes = engine.grammar.FindNodesByWordId(terms.First().id);
                if (nodes.Count == 0) continue;
                Console.WriteLine("Продолжение");
                var next = nodes.First().Followers.Keys;
                Console.WriteLine(string.Join("\n", next.Select(t => engine.ToTerm(t))));
                //var nearest = terms.SelectMany(t => engine.Nearest(terms.First(), 4)).Distinct().ToList();
                //Console.WriteLine("\nСовместные:\n" + nearest.Aggregate("", (c, n) => c + $"\n" + "[" + n.confidence.ToString("F4") + "] " + n.ToString()));
            }
            Console.WriteLine("\n\nНажмите любую клавишу для продолжения");
            Console.ReadKey();
            //Отключаемся от хранилища
            //l.Disconnect();
        }

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
