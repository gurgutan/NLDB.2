using System;
using System.Collections.Generic;
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
            Language l = new Language("Wiki.ru", new string[] { "", @"[^\w\d]+", @"[\n\r]+", @"\[\[\d+\]\]" });
            Console.WriteLine($"Начало обучения на файле {trainfile}");
            using (StreamReader reader = File.OpenText(trainfile)) l.Create(reader);
            l.BuildSequences();
            Console.WriteLine();
            Console.WriteLine($"Слов: {l.Count}");
            Console.WriteLine("Сохранение в БД");
            l.Save("words.db");
            Console.WriteLine("Поиск в БД по id");
            l.Connect("words.db");
            List<int[]> childsList = new List<int[]>();
            for (int i = 64; i < 128; i++)
            {
                Word w = l.Find(i);
                var childs = w.childs;
                if (childs != null)
                    childsList.Add(childs);
                Console.WriteLine(i.ToString() + $"={w.id}<{w.rank}>:" + l.ToTerm(w).ToString());
            }
            Console.WriteLine("Поиск в БД по childs");
            childsList.ForEach(e =>
            {
                Word w = l.Find(e);
                if (w != null)
                    Console.WriteLine($"{w.id}<{w.rank}>:" + l.ToTerm(w).ToString());
                else Console.WriteLine("Не найден " + e.Aggregate("", (c, n) => c + "," + n.ToString()));
            });
            l.Disconnect();
            TestLangConsole(l);
        }

        static void TestLangConsole(Language l)
        {
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

                Console.WriteLine(splitline + "\nРаспознавание ");
                var terms = l.Similars(text, 4, 2).ToList();
                terms.ForEach(term => { if (term.id >= 0) Console.WriteLine($"{term.confidence}: {term.ToString()}"); });

                Console.WriteLine(splitline + "\nПредположение о следующем слове: ");
                var predicted_one = l.Predict(text, 2);
                if (predicted_one != null)
                    Console.WriteLine(predicted_one.confidence + ": " + predicted_one.ToString());

                Console.WriteLine(splitline + "\nПостроение цепочки");
                var predicted = l.PredictRecurrent(text, 64, 2);
                if (predicted.Count != 0)
                    Console.WriteLine(predicted.Aggregate("", (c, n) => c + " " + n.ToString()));
            }
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
