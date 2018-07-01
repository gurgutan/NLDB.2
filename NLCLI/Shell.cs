using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using NLDB;
using System.IO;

namespace NLDB.NLCLI
{
    public enum ShellExecuteMode { Debug, Standart };


    public class Shell
    {
        public const string Version = "0.01";
        public readonly string WelcomeMessage = "Оболочка командной строки NLDB " + Version + "\nДля подсказки используйте команду 'help'";
        public string PromptDelimiter = ">>";

        Language DB;

        public ShellExecuteMode ExecuteMode = ShellExecuteMode.Debug;

        public Shell() { }

        public Shell(Language db)
        {
            DB = db;
        }

        public void DialogLoop()
        {
            Command c = new Command();
            ShowWelcomeMessage();
            while (c.CommandType != CommandTypes.Quit)
            {
                this.ShowPrompt();
                string line = Console.ReadLine();
                if (c.TryParse(line))
                    this.Execute(c);
                else ErrorMessage("Ошибка интерпретации команды. Воспользуйтесь командой help");
            }
        }

        private void ShowWelcomeMessage()
        {
            Console.WriteLine(WelcomeMessage);
        }

        private void ShowPrompt()
        {
            string dbname = (DB == null) ? "null" : DB.Name;
            Console.Write(dbname + PromptDelimiter);
        }

        public int Execute(Command c)
        {
            switch (c.CommandType)
            {
                case CommandTypes.Quit: return 0;
                case CommandTypes.Help: return ShowHelp();
                case CommandTypes.Clear: return ClearDB();
                case CommandTypes.Create: return CreateDB(c.Parameters);
                case CommandTypes.Find: return FindInDB(c.Parameters);
                case CommandTypes.Empty: return 0;
                default: { NotImplementedCommandType(); return 1002; }
            }
        }

        private int ShowHelp()
        {
            foreach (var s in Command.helpstrings)
                Console.WriteLine(s);
            return 0;
        }

        private int FindInDB(Dictionary<string, string> parameters)
        {
            if (DB == null)
            {
                ErrorMessage("База данных не инициализирована. Сначала загрузите или создайте БД.");
                return 0;
            }
            string text = "";
            int rank = 1;
            int maxcount = 1;
            foreach (var key in parameters.Keys)
                switch (key)
                {
                    case "text": text = parameters[key]; break;
                    case "rank":
                        {
                            if (!int.TryParse(parameters[key], out rank))
                                IncorrectParameter(key, parameters[key]);
                            return 1001;
                        }
                    case "top":
                        {
                            if (!int.TryParse(parameters[key], out maxcount))
                                IncorrectParameter(key, parameters[key]);
                            return 1001;
                        }
                    default: UnknownParameter(key); break;
                }
            //Вывод информации о команде
            Stopwatch stopwatch = Stopwatch.StartNew();
            this.DebugMessage($"[{stopwatch.Elapsed.ToString()}] поиск ближайших {maxcount} к '{text}' в лексиконе ранга {rank}");
            var terms = DB.FindMany(text, maxcount, rank);
            stopwatch.Stop();
            this.DebugMessage($"[{stopwatch.Elapsed.ToString()}] завершено");
            ShowTerms(terms);
            return 0;
        }

        //Пара [ключ,значение] словаря соответствуют типу источника и пути к источнику. Допустимы варианты:
        //[file, полное_имя_файла]
        //[folder, полный_путь_к_папке]
        //[string, строка_с_данными]
        //TODO: сделать возможной обработку данных из Stream
        private int CreateDB(Dictionary<string, string> parameters)
        {
            string name = "";
            List<string> splitters = new List<string>();
            foreach (var key in parameters.Keys)
                switch (key)
                {
                    case "name": name = parameters[key]; break;
                    case "splitters": splitters.Add(parameters[key]); break;
                    default: { UnknownParameter(key); return 1001; };
                }
            DB = new Language(name, splitters.ToArray());
            return 0;
        }

        private int AddData(Dictionary<string, string> parameters)
        {
            if (DB == null)
            {
                ErrorMessage("База данных не инициализирована. Сначала загрузите или создайте БД.");
                return 0;
            }
            foreach (var key in parameters.Keys)
                switch (key)
                {
                    case "file": return AddDataFromFile(parameters[key]);
                    case "folder": return AddDataFromFolder(parameters[key]);
                    case "string": return AddDataFromString(parameters[key]);
                    default: { UnknownParameter(key); return 1001; };
                }
            return 0;
        }

        private int AddDataFromString(string s)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            this.DebugMessage($"[{stopwatch.Elapsed.ToString()}] Добавление данных из строки");
            int wordscount = DB.CreateFromString(s);
            stopwatch.Stop();
            Console.WriteLine($"В БД {DB.Name} добавлено {wordscount} слов.");
            this.DebugMessage($"[{stopwatch.Elapsed.ToString()}] завершено");
            return 0;
        }

        private int AddDataFromFolder(string path)
        {
            int wordscount = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
            this.DebugMessage($"[{stopwatch.Elapsed.ToString()}] Добавление данных из папки '{path}'");
            var files = Directory.GetFiles(path);
            foreach(var file in files)
                wordscount += DB.CreateFromTextFile(file);
            Console.WriteLine($"В БД {DB.Name} добавлено {wordscount} слов.");
            this.DebugMessage($"[{stopwatch.Elapsed.ToString()}] завершено");
            return 0;
        }

        private int AddDataFromFile(string filename)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            this.DebugMessage($"[{stopwatch.Elapsed.ToString()}] Добавление данных из файла '{filename}'");
            int wordscount = DB.CreateFromTextFile(filename);
            stopwatch.Stop();
            Console.WriteLine($"В БД {DB.Name} добавлено {wordscount} слов.");
            this.DebugMessage($"[{stopwatch.Elapsed.ToString()}] завершено");
            return 0;
        }

        private int ClearDB()
        {
            if (DB == null)
            {
                Console.WriteLine("База данныйх не инициализирована");
                return 0;
            }
            DB.Clear();
            return 0;
        }

        private void IncorrectParameter(string key, string value)
        {
            Console.WriteLine($"Некорректный параметр {key}:{value}");
        }

        private void UnknownParameter(string key)
        {
            Console.WriteLine($"Неизвестный параметр {key}");
        }

        private void DebugMessage(string s)
        {
            if (this.ExecuteMode == ShellExecuteMode.Debug)
                Console.WriteLine(s);
        }

        private void ErrorMessage(string s)
        {
            Console.WriteLine(s);
        }

        private void ShowTerms(IEnumerable<Term> terms)
        {
            terms.ToList().ForEach(term =>
            {
                if (term.Id >= 0)
                    Console.WriteLine($"[{term.Confidence}, {DB.Lexicons[term.Rank].WordIdToText(term.Id)}]");
            });
        }

        private void NotImplementedCommandType()
        {
            Console.WriteLine("Данный тип команды не реализован");
        }
    }
}
