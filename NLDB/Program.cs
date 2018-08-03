using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    class Program
    {
        static string splitline = "----------------------------------------------------------------------";

        static void Main(string[] args)
        {
            string trainfile = @"D:\Data\Wiki\ru\23mb.txt";
            Language l = new Language("wikiru.db", new string[] { "", @"[^а-яёa-z\%\d]+", @"[\n\r]+", @"\[\[\d+\]\]" });
            //Console.WriteLine($"Начало обучения на файле {trainfile}");
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            //using (StreamReader reader = File.OpenText(trainfile))
            //    l.Build(reader);
            //sw.Stop();
            //Debug.WriteLine(sw.Elapsed.TotalSeconds + " sec");
            //l.Connect("wikiru.db");
            l.BuildGrammar();
            
            Console.WriteLine($"Слов: {l.Count}");
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

        static void TestLangConsole(Language l)
        {
            l.Connect(l.Name);
            Queue<string> lines = new Queue<string>();
            int que_size = 1;
            string line = "---";
            while (line != "")
            {
                Console.WriteLine(splitline);
                Console.Write(">>");
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
                var next = l.Next(text, 2);
                sw.Stop();
                Console.WriteLine(sw.Elapsed.TotalSeconds + " sec");
                if (next.Count != 0)
                    Console.WriteLine(next.Aggregate("", (c, n) => c + $" " + n.ToString()));
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

        static void NormilizeTest()
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
            foreach (var s in result)
                Console.Write(s + "\n------------------------------\n");
            Console.ReadKey();
        }

    }
}
