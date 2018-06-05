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
            Console.ReadKey();
        }


        static void COOMatrixTest()
        {
            string filename = @"D:\Data\Text\test1.txt";
            Language lang1 = new Language("Русские слова", new string[] { "", @"[^а-яА-ЯёЁ0-9]", @"[\.\?\!\n\r]" });
            lang1.CreateFromTextFile(filename);
            foreach (var lex in lang1.Lexicons)
            {
                Console.WriteLine($"Слов ранга {lex.Rank}: {lex.Count}");
                //lang1[0].Alphabet.OrderBy(s => s).ToList().ForEach(s => Console.Write(s + " "));
            }
            int[][] m = lang1[1].AsCOOMatrix();
            int rows = m[1].Max() + 1;
            int cols = m[2].Max() + 1;
            int[,] dm = new int[rows, cols];
            for (int i = 0; i < m[0].Length; i++)
                dm[m[1][i], m[2][i]] = m[0][i];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    Console.Write(dm[i, j] + " ");
                Console.WriteLine();
            }
            Console.WriteLine(m[0].Length);
            Console.ReadKey();
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
