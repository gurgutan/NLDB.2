using System;
using System.Text;

namespace NLDB
{
    internal class ProgressInformer
    {
        public readonly int DefaultBarSize = 48;
        public string Prompt { get; set; }
        public bool ShowProgressBar { get; set; }
        public bool ShowPercents { get; set; }
        public int BarSize { get; set; }
        public long Max { get; set; }
        public long Current { get; set; }

        public int Pos { get; private set; }
        public int LeftBorderPos { get; private set; }

        private readonly char[] animationChars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '*' };
        private readonly char lineChar = '=';
        private readonly char leftBorder = '[';
        private readonly char rightBorder = ']';

        public ProgressInformer(string prompt, long max)
        {
            Prompt = prompt;
            Max = max;
            ShowProgressBar = true;
            ShowPercents = true;
            BarSize = DefaultBarSize;
            LeftBorderPos = Prompt.Length + 5;
            Current = 0;
            Console.Write($"{Prompt}  0%{leftBorder}{" ".PadLeft(BarSize)}{rightBorder}");
        }

        public long Inc(long count)
        {
            Current += count;
            double percents = Math.Round(100.0 / Max * Current, 1);
            int parts = (int)((percents - Math.Truncate(percents)) * 10);
            Pos = (int)(BarSize / Max * Current);
            Console.CursorLeft = Pos;
            StringBuilder strBuilder = new StringBuilder();
            if (Pos > 0) strBuilder.Append(lineChar, Pos - 1);
            string progress = strBuilder.ToString().PadRight(BarSize);
            Console.CursorLeft = 0;
            Console.Write($"{Prompt} {((int)percents).ToString().PadLeft(2)}%{leftBorder}{progress}{animationChars[parts]}{rightBorder}");
            return Current;
        }

    }
}
