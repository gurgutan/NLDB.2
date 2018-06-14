using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;

namespace NLDB
{
    class Program
    {
        static void Main(string[] args)
        {

            //Language l = TestDeserializing();
            //TestLangConsole(l);
            TestLanguage();
            //SplitTest();
        }

        private static Language TestDeserializing()
        {
            string datafile = @"D:\Data\Lang.dat";
            Language l = Language.Deserialize(datafile);
            return l;
        }

        static void TestLangConsole(Language l)
        {
            Random rand = new Random((int)DateTime.Now.Ticks);
            foreach (var lex in l.Lexicons)
            {
                Console.WriteLine($"Слов ранга {lex.Rank}: {lex.Count}");
                Console.WriteLine(lex.ToText(rand.Next(lex.Count)));
                Console.WriteLine("-----------------------------------------------------------");
            }
            string line = "---";
            while (line != "")
            {
                Console.Write(">>");
                line = Console.ReadLine();
                var term = l.Eval(line, 2);
                if (term.Id >= 0)
                {
                    Console.WriteLine(l.Lexicons[term.Rank].ToText(term.Id));
                    Console.WriteLine(term.Confidence);
                }
            }
        }
        static void TestLanguage()
        {
            Random rand = new Random((int)DateTime.Now.Ticks);
            string trainfile = @"D:\Data\Wiki\ru\254kb.txt";
            //string trainfile = @"D:\Data\Text\philosoph1.txt";
            Language l = new Language("Русские слова", new string[] { "", @"[^\w\d]+", @"[\:\;\.\?\!\n\r]+", @"\[\[\d+\]\]" });
            l.CreateFromTextFile(trainfile);
            foreach (var lex in l.Lexicons)
            {
                Console.WriteLine($"Слов ранга {lex.Rank}: {lex.Count}");
                Console.WriteLine(lex.ToText(rand.Next(lex.Count)));
                Console.WriteLine("-----------------------------------------------------------");
            }

            string line = "---";
            while (line != "")
            {
                Console.Write(">>");
                line = Console.ReadLine();
                var term = l.Eval(line, 2);
                if (term.Id >= 0)
                {
                    Console.WriteLine(l.Lexicons[term.Rank].ToText(term.Id));
                    Console.WriteLine(term.Confidence);
                }
            }
        }

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
