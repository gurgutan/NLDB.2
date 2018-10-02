using System;
using System.Text;

namespace NLDB
{
    internal class ProgressInformer
    {
        public readonly int DefaultBarSize = 64;
        public string Prompt { get; set; }
        public bool ShowProgressBar { get; set; }
        public bool ShowPercents { get; set; }
        public int BarSize { get; set; }
        public long Max { get; set; }
        private long current;
        public long Current
        {
            get => this.current;
            set => this.current = value;
        }

        public int Pos { get; private set; }
        public int LeftBorderPos { get; private set; }

        private readonly char[] animationChars = new char[] { '-', '+', '*', 'O', '*', '+' };
        private readonly char lineChar = '=';
        private readonly char leftBorder = '[';
        private readonly char rightBorder = ']';
        private int animateFrame = 0;

        public ProgressInformer(string prompt, long max)
        {
            this.Prompt = prompt;
            this.Max = max;
            this.ShowProgressBar = true;
            this.ShowPercents = true;
            this.BarSize = this.DefaultBarSize;
            this.LeftBorderPos = this.Prompt.Length + 5;
            this.Current = 0;
        }

        public long Inc(long count)
        {
            this.Current += count;
            return this.Current;
        }

        public void Show()
        {
            double percents = Math.Truncate(100.0 / this.Max * this.Current);
            //int parts = (int)((percents - Math.Truncate(percents)) * this.animationChars.Length);
            this.Pos = (int)(this.BarSize / (double)this.Max * this.Current);
            this.animateFrame++;
            StringBuilder strBuilder = new StringBuilder();
            if (this.Pos > 0)
                strBuilder.Append(this.lineChar, this.Pos);
            if (this.BarSize - this.Pos > 0)
                strBuilder.Append(this.animationChars[this.animateFrame % this.animationChars.Length]);
            if (this.BarSize - this.Pos > 0 && this.BarSize - strBuilder.ToString().Length + 1 > 0)
                strBuilder.Append(' ', this.BarSize - strBuilder.ToString().Length + 1);
            string progress = strBuilder.ToString();
            string percentsStr = ((int)percents).ToString().PadLeft(2);
            Console.CursorLeft = 0;
            Console.Write($"{this.Prompt} {percentsStr}%{this.leftBorder}{progress}{this.rightBorder}");
        }

    }
}
