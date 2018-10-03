using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NLDB
{
    internal class Program
    {
        private static readonly string splitline = "---------------------------------------------------------------------------";

        private static void Main(string[] args)
        {
            string dbname = "wikiru5mb.db";
            string trainfile = @"D:\Data\Wiki\ru\5mb.txt";
            string[] splitters = new string[] { "", @"[^а-яё\d\{\}]+", @"[\n\r]+", @"\[\[{число}\]\]" };
            Language l = new Language(dbname, splitters);
            //l.CreateDB();
            l.Connect();
            Console.WriteLine($"Начало обучения на файле {trainfile}");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            //l.BuildLexicon(trainfile);
            l.BuildGrammar();
            sw.Stop();
            Debug.WriteLine(sw.Elapsed.TotalSeconds + " sec");
            //Console.WriteLine("Поиск в БД по id");
            //List<int[]> childsList = new List<int[]>();
            //for (int i = 64; i < 128; i++)
            //{
            //    Word w = l.Find(i);
            //    var childs = w.childs;
            //    if (childs != null)
            //        childsList.Add(childs);
            //    Console.WriteLine(i.ToString() + $"={w.id}<{w.rank}>:" + l.ToTerm(w).ToString());
            //}
            //Console.WriteLine("Поиск в БД по childs");
            //childsList.ForEach(e =>
            //{
            //    Word w = l.Find(e);
            //    if (w != null)
            //        Console.WriteLine($"{w.id}<{w.rank}>:" + l.ToTerm(w).ToString());
            //    else Console.WriteLine("Не найден " + e.Aggregate("", (c, n) => c + "," + n.ToString()));
            //});
            //l.Disconnect();
            TestLangConsole(l);
        }

        private static void TestLangConsole(Language l)
        {
            int rank = 2;
            if (!l.IsConnected()) l.Connect();
            Queue<string> lines = new Queue<string>();
            int que_size = 1;
            string line = splitline;
            Console.WriteLine();
            while (line != "")
            {
                Console.WriteLine(splitline);
                Console.Write($"{rank}>>");
                line = Console.ReadLine();
                if (line == "") continue;
                lines.Enqueue(line);
                if (lines.Count > que_size) lines.Dequeue();
                string text = lines.Aggregate("", (c, n) => c == "" ? n : c + "\n" + n);

                //Console.WriteLine(splitline + "\nРаспознавание ");
                Stopwatch sw = new Stopwatch();
                //sw.Start();
                //var terms = l.Similars(text, 4, 2).ToList();
                //sw.Stop();
                //Console.WriteLine(sw.Elapsed.TotalSeconds + " sec");
                //terms.ForEach(term => { if (term.id >= 0) Console.WriteLine($"{term.confidence}: {term.ToString()}"); });
                //Console.WriteLine(splitline + "\nПредположение о следующем слове: ");
                //sw.Restart();
                //var predicted_one = l.Predict(text, 2);
                //sw.Stop();
                //Console.WriteLine(sw.Elapsed.TotalSeconds + " sec");
                //if (predicted_one != null) Console.WriteLine(predicted_one.confidence + ": " + predicted_one.ToString());
                //Console.WriteLine(splitline + "\nПостроение цепочки");
                //sw.Restart();
                //var predicted = l.PredictRecurrent(text, 32, 2);
                //sw.Stop();
                //Console.WriteLine(sw.Elapsed.TotalSeconds + " sec");
                //if (predicted.Count != 0)
                //    Console.WriteLine(predicted.Aggregate("", (c, n) => c + $" [{n.confidence.ToString("F2")}] " + n.ToString()));
                Console.WriteLine(splitline + "\nПостроение цепочки");
                sw.Restart();
                List<Term> next = l.Next(text, rank);
                sw.Stop();
                Console.WriteLine(sw.Elapsed.TotalSeconds + " sec");
                if (next.Count != 0)
                    Console.WriteLine(next.Aggregate("", (c, n) => c + $" " + n.ToString()));
                //l.FreeMemory();
            }
            l.Disconnect();
        }

        //private static void TestDeserialization()
        //{
        //    string filename = @"D:\Data\SerializeLanguageTest.dat";
        //    Console.WriteLine($"Десериализация из файла {filename}");
        //    Language l = Language.Deserialize(filename);
        //    TestLangConsole(l);
        //}

        //private static void TestDBSave()
        //{
        //    string filename = @"D:\Data\SaveLanguageTest.db";
        //    string trainfile = @"D:\Data\Wiki\ru\5mb.txt";
        //    Language l = new Language("Wiki.ru", new string[] { "", @"[^\w\d]+", @"[\:\;\.\?\!\n\r]+", @"\[\[\d+\]\]" });
        //    l.CreateFromTextFile(trainfile);
        //    Console.WriteLine($"Сохранение в БД {filename}");
        //    l.DBSave(filename);
        //    Console.ReadKey();
        //}

        //private static void TestSerialization()
        //{
        //    string filename = @"D:\Data\SerializeLanguageTest.dat";
        //    string trainfile = @"D:\Data\Wiki\ru\5mb.txt";
        //    Language l = new Language("Wiki.ru", new string[] { "", @"[^\w\d]+", @"[\:\;\.\?\!\n\r]+", @"\[\[\d+\]\]" });
        //    l.CreateFromTextFile(trainfile);
        //    Console.WriteLine($"Сериализация в файл {filename}");
        //    l.Serialize(filename);
        //}

        //static void TestLanguage()
        //{
        //    Random rand = new Random((int)DateTime.Now.Ticks);
        //    string trainfile = @"D:\Data\Wiki\ru\23mb.txt";
        //    //string trainfile = @"D:\Data\Text\philosoph1.txt";
        //    Language l = new Language("Wiki.ru", new string[] { "", @"[^\w\d]+", @"[\:\;\.\?\!\n\r]+", @"\[\[\d+\]\]" });
        //    l.CreateFromTextFile(trainfile);
        //    foreach (var lex in l.Lexicons)
        //    {
        //        Console.WriteLine($"Слов ранга {lex.Rank}: {lex.Count}");
        //        Console.WriteLine(lex.WordIdToText(rand.Next(lex.Count)));
        //        Console.WriteLine(splitline);
        //    }
        //    TestLangConsole(l);
        //}

        private static void NormilizeTest()
        {
            Parser parser = new Parser(@"[^а-яА-ЯёЁ0-9]");
            string result = parser.Normilize("Привет ~~м^^ир/!");
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
