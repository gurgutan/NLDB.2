using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;

namespace NLDB
{
    class Program
    {

        static void Main(string[] args)
        {
            Random rand = new Random((int)DateTime.Now.Ticks);
            string trainfile = @"D:\Data\philosoph1.txt";
            Language l = new Language("Русские слова", new string[] { "", @"[^а-яА-ЯёЁ0-9]", @"[\.\?\!\n\r]" });
            l.CreateFromTextFile(trainfile);
            foreach (var lex in l.Lexicons)
            {
                Console.WriteLine($"Слов ранга {lex.Rank}: {lex.Count}");
                Console.WriteLine(lex.AsText(rand.Next(lex.Count)));
            }

            string line = "";
            while (line != "q")
            {
                Console.Write(">>");
                line = Console.ReadLine();
                var term = l.Eval(line);
                if (term.Id >= 0)
                {
                    Console.WriteLine(l.Lexicons[term.Rank].AsText(term.Id));
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
            Parser parser = new Parser(@"[^а-яА-ЯёЁ0-9]");
            string[] result = parser.Split("Привет мир!");
            foreach (var s in result)
                Console.Write(s + " ");
        }

    }
}
