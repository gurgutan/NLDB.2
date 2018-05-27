using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    class Program
    {
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

        static void Main(string[] args)
        {
            //NormilizeTest();
            //SplitTest();
            string filename = @"D:\Data\philosoph1.txt";
            Language lang1 = new Language("Русские слова", new string[] { "", @"[^а-яА-ЯёЁ0-9]", @"[\.\?\!\n\r]" });
            lang1.CreateFromTextFile(filename);
            foreach (var i in lang1[2].Codes)
                Console.WriteLine(lang1[2].AsText(i));
            foreach (var lex in lang1.Lexicons)
            {
                Console.WriteLine($"Слов ранга {lex.Rank}: {lex.Count}");
                //lang1[0].Alphabet.OrderBy(s => s).ToList().ForEach(s => Console.Write(s + " "));
            }

            Console.ReadKey();
        }
    }
}
