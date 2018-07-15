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
            string trainfile = @"D:\Data\Wiki\ru\100mb.txt";
            Language l = new Language("Wiki.ru", new string[] { "", @"[^\w\d]+", @"[\n\r]+", @"\[\[\d+\]\]" });
            Console.WriteLine($"Начало обучения на файле {trainfile}");
            using (StreamReader reader = File.OpenText(trainfile)) l.Create(reader);
            l.BuildSequences();
            Console.WriteLine();
            Console.WriteLine($"Слов: {l.Count}");
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
                string text = lines.Aggregate("", (c, n) => c == "" ? n : c + "." + n);
                //var terms = l.Similars(text, 2, count: 4).ToList();
                //terms.ForEach(term => { if (term.id >= 0) Console.WriteLine($"{term.confidence}: {term.ToString()}"); });
                var predicted = l.PredictRecurrent(text, 16);
                if (predicted.Count != 0)
                    Console.WriteLine(predicted.Aggregate("",(c,n)=>c+" "+n.ToString()));
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
