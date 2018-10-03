using System;
using System.Text;

namespace NLDB
{
    internal class ProgressInformer
    {
        public readonly int DefaultBarSize = 32;
        public string Prompt { get; set; }
        public bool ShowProgressBar { get; set; }
        public bool ShowPercents { get; set; }
        public bool ShowCurrent { get; set; }
        public int BarSize { get; set; }
        public long Max { get; set; }
        private long current;
        public long Current
        {
            get => this.current;
            set => this.current = value;
        }

        public int LeftBorderPos { get; private set; }

        private readonly char[] animationChars = new char[] { '|', '}', '>', '-' };
        private readonly char lineChar = '=';
        private readonly char leftBorder = '[';
        private readonly char rightBorder = ']';
        private int animateFrame = 0;
        private readonly int PosY;
        private readonly int PosX;

        public ProgressInformer(string prompt, long max)
        {
            Prompt = prompt;
            Max = max;
            ShowProgressBar = true;
            ShowPercents = true;
            ShowCurrent = true;
            BarSize = Math.Min(Console.BufferWidth - Prompt.Length - 7, this.DefaultBarSize);
            LeftBorderPos = Prompt.Length + 5;
            Current = 0;
            this.PosX = Console.CursorLeft;
            this.PosY = Console.CursorTop;
        }

        public long Inc(long count)
        {
            Current += count;
            return Current;
        }

        public void Show()
        {
            double percents = Math.Truncate(100.0 / Max * Current);
            //int parts = (int)((percents - Math.Truncate(percents)) * this.animationChars.Length);
            int barPos = (int)(BarSize / (double)Max * Current);
            this.animateFrame++;
            StringBuilder strBuilder = new StringBuilder();
            if (barPos > 0)
                strBuilder.Append(this.lineChar, barPos);
            if (BarSize - barPos > 0)
                strBuilder.Append(this.animationChars[this.animateFrame % this.animationChars.Length]);
            if (BarSize - barPos > 0 && BarSize - strBuilder.ToString().Length + 1 > 0)
                strBuilder.Append(' ', BarSize - strBuilder.ToString().Length + 1);
            string progress = strBuilder.ToString();
            string percentsStr = ((int)percents).ToString().PadLeft(2);
            Console.CursorLeft = this.PosX;
            Console.CursorTop = this.PosY;
            Console.Write($"{Prompt}");
            if (ShowPercents) Console.Write($"{percentsStr}%");
            if (ShowProgressBar) Console.Write($"{this.leftBorder}{progress}{this.rightBorder}");
            if (ShowCurrent) Console.Write($"{Current}\\{Max}");
        }

        public void Set(int n)
        {
            this.current = n;
            Show();
        }
    }
}
