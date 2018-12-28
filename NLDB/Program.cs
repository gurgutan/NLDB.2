using System;
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
            Engine engine = new Engine(@"D:\Data\Result\Engine_5mb.db");
            engine.Create();
            engine.Insert(new Splitter(0, ""));
            engine.Insert(new Splitter(1, @"[^а-яё\d\{\}\-]+"));
            engine.Insert(new Splitter(2, @"[\n\r\:\;]+"));
            engine.Insert(new Splitter(3, @"\[\[{число}\]\]"));

            engine.ExecuteMode = ExecuteMode.Verbose;
            engine
                .Execute(OperationType.FileReading, @"D:\Data\Wiki\ru\5mb.txt")
                .Then(OperationType.TextNormalization)
                .Then(OperationType.TextSplitting)
                .Then(OperationType.WordsExtraction);
            engine.Execute(OperationType.FileWriting, @"D:\Data\Wiki\ru\5mb.Words");
            //.Then(ProcessingType.WordsMean)
            //.Then(ProcessingType.WordsSimilarity);

            //После создания объекта создаем хранилище. Это нужно так как к созданному ранее хранилищу можно сразу подключиться
            //l.Create();
            //Подключимся к хранилищу
            //l.Connect();
            //Console.WriteLine($"Начало обучения на файле {trainfile}");
            //Запускаем процесс построения структуры текста
            //l.Preprocessing(trainfile, Language.ProcessingType.Build);
            //l.Preprocessing(trainfile, Language.ProcessingType.Distance);
            //l.Preprocessing(trainfile, Language.ProcessingType.Similarity);
            //Теперь будем использовать полученные данные
            Console.WriteLine("\n\nДля окончания диалога нажмите Enter");
            string line = "-";
            while (line != "")
            {
                if (line == "") continue;
                Console.Write($"\n\nФраза: ");
                line = Console.ReadLine();
                List<Term> words = engine.Recognize(text: line, count: 8);
                Console.WriteLine("\n\nПохожие предложения:\n" + words.Aggregate("", (c, n) => c + $"\n" + n.ToString()));
                List<Term> similars = engine.Similars(text: line, count: 8);
                //Найдем 8 лучших совпадений с текстом line. rank=2 означает, что нас интересуют совпадения предложений
                //List<Term> similars = l.Similars(text: line, rank: 2, count: 8);
                //Console.WriteLine("\n\nПохожие предложения:\n" + similars.Aggregate("", (c, n) => c + $"\n" + n.ToString()));
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

        private static void TestLangConsole(Language l)
        {
            int rank = 2;
            if (!l.IsConnected()) l.Connect();
            Queue<string> lines = new Queue<string>();
            int que_size = 1;
            string line = ">>";
            Console.WriteLine();
            while (line != "")
            {
                Console.WriteLine();
                Console.Write($"{rank}>>");
                line = Console.ReadLine();
                if (line == "") continue;
                lines.Enqueue(line);
                if (lines.Count > que_size) lines.Dequeue();
                string text = lines.Aggregate("", (c, n) => c == "" ? n : c + "\n" + n);
                Stopwatch sw = new Stopwatch();
                Console.WriteLine("\nПостроение цепочки");
                sw.Restart();
                IEnumerable<Term> core = l.GetCore(text, rank: 2);
                //List<Term> next = l.Next(text, rank);
                sw.Stop();
                Console.WriteLine(sw.Elapsed.TotalSeconds + " sec");
                if (core.Count() != 0)
                    Console.WriteLine(core.Aggregate("", (c, n) => c + $"\n" + n.ToString()));
                //if (next.Count != 0)
                //    Console.WriteLine(next.Aggregate("", (c, n) => c + $" " + n.ToString()));
                //l.FreeMemory();
            }
            l.Disconnect();
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
