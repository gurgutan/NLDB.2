using System;
using System.Text;

namespace NLDB.Utils
{
    /// <summary>
    /// Класс для отображения полосы прогрессы операции в консольном режиме
    /// </summary>
    internal class ProgressInformer : IDisposable
    {
        private static readonly object ConsoleWriterLock = new object();
        /// <summary>
        /// Размер полосы по умолчанию в символах
        /// </summary>
        public const int DefaultBarSize = 32;
        /// <summary>
        /// Текст перед полосой
        /// </summary>
        public string Prompt { get; set; }
        /// <summary>
        /// Единицы измерения (выводятся вместе с числовым значением после полосы)
        /// </summary>
        public string UnitsOfMeasurment { get; set; }
        /// <summary>
        /// При значении true выводит полосу прогресса
        /// </summary>
        public bool ShowProgressBar { get; set; }
        /// <summary>
        /// При значении true - выводит значение завершенности в процентах
        /// </summary>
        public bool ShowPercents { get; set; }
        /// <summary>
        /// При значении true - выводит текущее значение завершенности в реальных единицах
        /// </summary>
        public bool ShowCurrent { get; set; }
        /// <summary>
        /// Размер полосы в символах (без учета остальных элементов - скобок, процентов и т.п.)
        /// </summary>
        public int BarSize { get; set; }
        /// <summary>
        /// Максимальное значение величины в реальных единицах
        /// </summary>
        public long Max { get; set; }

        /// <summary>
        /// Желаемое максимальное число кадров в секунду для анимации прогресса.
        /// </summary>
        public int FPS { get; set; }

        private long current;
        private DateTime prevTime;
        private TimeSpan currentTimeSpan;
        /// <summary>
        /// Текущее значение величины в реалбных единицах
        /// </summary>
        public long Current
        {
            get => current;
            set => current = value;
        }

        private readonly char[] animationChars = new char[] { '|', '/', '-', '\\' };    //символы анимирующие правый край полосы
        private readonly char lineChar = '#';   //символ заполняющий полосу
        private readonly char leftBorder = '['; //левый край полосы
        private readonly char rightBorder = ']';//правый край полосы
        private int animateFrame = 0;           //текущая позиция анимирующего символа в animationChars
        private readonly int PosY;
        private readonly int PosX;

        public ProgressInformer(string prompt = "", long max = 100, string measurment = "", int barSize = 64, int fps = 0)
        {
            if (fps < 0 || fps > 1000)
                throw new ArgumentOutOfRangeException("FPS должен быть в интервале [0,1000]");
            Prompt = prompt;
            UnitsOfMeasurment = measurment;
            Max = max;
            ShowProgressBar = true;
            ShowPercents = true;
            ShowCurrent = true;
            BarSize = Math.Min(Console.BufferWidth - Prompt.Length - 5 - max.ToString().Length * 2 - 1, barSize);
            FPS = fps;
            Current = 0;
            PosX = Console.CursorLeft;
            PosY = Console.CursorTop;
            prevTime = DateTime.Now;
        }

        /// <summary>
        /// Увеличивает величину на count единиц
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public long Inc(long count)
        {
            Current += count;
            return Current;
        }

        /// <summary>
        /// Выводит на экран полосу прогресса
        /// </summary>
        public void Show(bool alwaysShow = false)
        {
            if (!IsTimeToShow(alwaysShow)) return;
            lock (ConsoleWriterLock)
            {
                double percents = 100;
                if (Max != 0)
                    percents = Math.Truncate(100.0 / Max * Current);
                //int parts = (int)((percents - Math.Truncate(percents)) * this.animationChars.Length);
                int barPos = 0;
                if (Max != 0)
                    barPos = (int)(BarSize / (double)Max * Current);
                animateFrame++;
                StringBuilder strBuilder = new StringBuilder();
                if (barPos > 0)
                    strBuilder.Append(lineChar, barPos);
                if (BarSize - barPos > 0)
                    strBuilder.Append(animationChars[animateFrame % animationChars.Length]);
                if (BarSize - barPos > 0 && BarSize - strBuilder.ToString().Length + 1 > 0)
                    strBuilder.Append(' ', BarSize - strBuilder.ToString().Length + 1);
                string progress = strBuilder.ToString();
                string percentsStr = ((int)percents).ToString().PadLeft(2);
                Console.CursorLeft = PosX;
                Console.CursorTop = PosY;
                Console.Write($"{Prompt}");
                if (ShowPercents) Console.Write($"{percentsStr}%");
                if (ShowProgressBar) Console.Write($"{leftBorder}{progress}{rightBorder}");
                if (ShowCurrent) Console.Write($"{Current}\\{Max} {UnitsOfMeasurment}");
            }
        }

        private bool IsTimeToShow(bool alwaysShow = false)
        {
            if (FPS == 0 || alwaysShow) return true;
            currentTimeSpan = DateTime.Now - prevTime;
            long intervalForFPS = 1000 / FPS;
            if (currentTimeSpan.TotalMilliseconds / intervalForFPS > 0)
            {
                prevTime = DateTime.Now.AddMilliseconds(1000 - (long)(currentTimeSpan.TotalMilliseconds) % intervalForFPS);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Устанавливает текущую позицию величины в n и выводит на экран
        /// </summary>
        /// <param name="n"></param>
        public void Set(long n, bool alwaysShow = false)
        {
            current = n;
            Show(alwaysShow);
        }

        public void Dispose()
        {
            Console.WriteLine();
        }
    }
}
