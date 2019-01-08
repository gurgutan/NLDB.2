﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLDB.DAL;

namespace NLDB
{
    internal class Program
    {

        private static void Main(string[] args)
        {
            string trainfile = @"D:\Data\Wiki\ru\5mb.txt";
            string dbpath = @"D:\Data\Result\5mb.db";
            Engine engine = new Engine(dbpath)
            {
                ExecuteMode = ExecuteMode.Verbose
            };

            Stopwatch sw = new Stopwatch();
            Console.WriteLine("Создание...");
            SparseMatrix a = new SparseMatrix(
                Enumerable.Range(0, 1<<25).Select(i => Tuple.Create(i, 0, (double)i))
                );
            //SparseMatrix b = new SparseMatrix(
            //    Enumerable.Range(0, 1<<25).Select(i => Tuple.Create(i * 2, 0, (double)i))
            //    );
            sw.Start();
            Console.WriteLine("Вычисление...");
            a.Transpose();
            Console.WriteLine($"--");
            sw.Stop();
            Console.WriteLine(sw.Elapsed.TotalSeconds);
            Console.ReadKey();
            //engine.Create();
            //engine.Insert(new Splitter(0, ""));
            //engine.Insert(new Splitter(1, @"[^а-яё\d\{\}\-]+"));
            //engine.Insert(new Splitter(2, @"[\n\r\:\;]+"));
            //engine.Insert(new Splitter(3, @"\[\[{число}\]\]"));
            //engine
            //    .Execute(OperationType.FileReading, trainfile)
            //    .Then(OperationType.TextNormalization)
            //    .Then(OperationType.TextSplitting)
            //    .Then(OperationType.WordsExtraction);
            engine.Clear("MatrixA");
            engine.Execute(OperationType.DistancesCalculation, engine.Words(1));
            engine.Execute(OperationType.DistancesCalculation, engine.Words(2));
            engine.Execute(OperationType.DistancesCalculation, engine.Words(3));
            engine.Clear("MatrixB");
            engine.Execute(OperationType.SimilarityCalculation, 0);
            engine.Execute(OperationType.SimilarityCalculation, 1);
            engine.Execute(OperationType.SimilarityCalculation, 2);
            //engine.Execute(OperationType.FileWriting, Path.ChangeExtension(dbpath,"words"));

            //Теперь будем использовать полученные данные
            Console.WriteLine("\n\nДля окончания диалога нажмите Enter");
            string line = "-";
            while (line != "")
            {
                if (line == "") continue;
                Console.Write($"\n\nФраза: ");
                line = Console.ReadLine();
                //Console.WriteLine(engine.ToTerm(engine.DB.GetWord(int.Parse(line))).ToString());
                List<Term> similars = engine.Similars(text: line, rank: 2, count: 8);
                //Найдем 8 лучших совпадений с текстом line. rank=2 означает, что нас интересуют совпадения предложений
                //List<Term> similars = engine.Similars(text: line, rank: 2, count: 8);
                Console.WriteLine("\n\nПохожие предложения:\n" + similars.Aggregate("", (c, n) => c + $"\n" + n.ToString()));
                //Получим предположение о предложении, следующем за line
                //List<Term> next = l.Next(text: line, rank: 2);
                //Console.WriteLine("\n\nОтветные предложения:\n" + next.Aggregate("", (c, n) => c + $"\n" + n.ToString()));
                //Получим предположение о предложении, следующем за line другим способом
                //var next2 = l.NextNearest(text: line, rank: 2, count: 16);
                //if (next2.Count > 0)
                //    Console.WriteLine("\n\nСледующее предложение:\n" + next2.Aggregate("", (c, n) => c + $"\n" + n.ToString()));
                //else
                //    Console.Write("\n\nНе найдено подходящих продолжений");
                //List<Term> alike = l.Alike(text: line, rank: 1, count: 8);
                //if (alike.Count > 0)
                //    Console.WriteLine("\n\nПохожие по смыслу:\n" + alike.Aggregate("", (c, n) => c + $"\n" + n.ToString()));
                //else
                //    Console.Write("\n\nНе найдено подходящих слов");
                //Получим предоположение о сути статьи, в котором есть предложение, наиболее похожее на line
                //IEnumerable<Term> core = l.GetCore(text: line, rank: 2);
                //Console.WriteLine("\n\nЯдро текста статьи:\n" + core.Aggregate("", (c, n) => c + $"\n" + n.ToString()));
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
