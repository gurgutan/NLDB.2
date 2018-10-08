﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NLDB
{
    internal class Program
    {
        
        private static void Main(string[] args)
        {
            string dbname = "wikiru884.db";
            string trainfile = @"D:\Data\Wiki\ru\884mb.txt";
            string[] splitters = new string[] { "", @"[^а-яё\d\{\}]+", @"[\n\r]+", @"\[\[{число}\]\]" };
            Language l = new Language(dbname, splitters);
            //l.Create();
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
                List<Term> next = l.Next(text, rank);
                sw.Stop();
                Console.WriteLine(sw.Elapsed.TotalSeconds + " sec");
                if (next.Count != 0)
                    Console.WriteLine(next.Aggregate("", (c, n) => c + $" " + n.ToString()));
                //l.FreeMemory();
            }
            l.Disconnect();
        }

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
