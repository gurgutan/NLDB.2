using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NLDB.Utils
{
    public class Logger : IDisposable
    {
        private string Filename { get; set; }

        private readonly bool AppendFile;

        private StreamWriter stream = null;
        private readonly object streamBlockObject = new object();
        private Stopwatch stopwatch = new Stopwatch();

        public bool ShowTimeSpan { get; set; }

        public Logger(string filename = "log.txt", bool append = false, bool showTimeSpan = true)
        {
            Filename = filename;
            AppendFile = append;
            if (ShowTimeSpan)
                stopwatch.Start();
            Open();
        }

        private void Open()
        {
            try
            {
                stream = new StreamWriter(Filename, append: true, encoding: Encoding.UTF8)
                {
                    AutoFlush = true
                };
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка открытия файла на запись '{Filename}':" + e.Message);
            }
        }

        public void WriteLine(string text, bool consoleOn = false)
        {
            lock (streamBlockObject)
            {
                string timespan = "";
                if (ShowTimeSpan)
                {
                    stopwatch.Stop();
                    timespan = " ("+stopwatch.Elapsed.TotalSeconds.ToString()+")";
                    stopwatch.Reset();
                }
                string t = DateTime.Now.ToString("dd.mm.yy hh:mm:ss.ffff") + $"{timespan}" + "> " + text;
                stream.WriteLine(t);
                if (consoleOn) Console.WriteLine(t);
            }
        }

        public void Dispose()
        {
            ((IDisposable)stream).Dispose();
        }
    }
}
