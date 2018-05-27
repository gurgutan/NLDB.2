using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    class Program
    {
        static void Main(string[] args)
        {
            string filename = @"D:\Data\Wiki\ru\100mb.txt";
            Language lang1 = new Language("Русские слова", 1, new string[] { "", @"[^а-яА-ЯёЁ0-9]" });
            lang1.CreateFromTextFile(filename);
            foreach (var lex in lang1.Lexicons)
            {
                Console.WriteLine($"Слов ранга {lex.Rank}: {lex.Count}");
                //lang1[0].Alphabet.OrderBy(s => s).ToList().ForEach(s => Console.Write(s + " "));
            }
            Console.ReadKey();
        }
    }
}
