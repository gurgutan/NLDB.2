using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NLDB.NLCLI
{
    public enum ShellExecuteMode { Debug, Standart };

    /// <summary>
    /// Класс командной оболочки. Предоставляет методы для исполнения команд пользователя в интерактивном режиме
    /// </summary>
    public class Shell
    {
        public const string Version = "0.01";
        public readonly string WelcomeMessage = "Оболочка командной строки NLDB " + Version + "\nДля подсказки используйте команду 'help'";
        public string PromptDelimiter = ">>";

        private Language db = null;
        private readonly int[] ErrorCodes = { 1001, 1002, 1003, 1004 };

        public ShellExecuteMode ExecuteMode = ShellExecuteMode.Debug;

        public Shell() { }

        public Shell(Language _db)
        {
            db = _db;
        }

        /// <summary>
        /// Метод входит в основной цикл диалога командной строки с пользователем.
        /// </summary>
        public void DialogLoop()
        {
            //TODO: Сделать сохранение последовательности команд в отдельный файл
            //TODO: Сделать сохранение лога работы
            Command c = new Command();
            ShowWelcomeMessage();
            while (c.CommandType != CommandTypes.Quit)
            {
                ShowPrompt();
                string line = Console.ReadLine();
                if (c.TryParse(line))
                {
                    int resultCode = Execute(c);
                    if (ErrorCodes.Contains(resultCode))
                        ErrorMessage($"Ошибка выполнения команды: {resultCode}");
                    else
                        InfoMessage($"Команда выполнена с кодом возврата {resultCode}");
                }
                else ErrorMessage("Ошибка интерпретации команды. Воспользуйтесь командой help");
            }
        }

        private void ShowWelcomeMessage()
        {
            Console.WriteLine(WelcomeMessage);
        }

        private void ShowPrompt()
        {
            string dbname = (db == null) ? "null" : db.Name;
            Console.Write(dbname + PromptDelimiter);
        }

        public int Execute(Command c)
        {
            switch (c.CommandType)
            {
                case CommandTypes.Quit: return 0;
                case CommandTypes.Help: return ShowHelp();
                case CommandTypes.Create: return Create(c.Parameters);
                case CommandTypes.Build: return Build(c.Parameters);
                case CommandTypes.Connect: return Connect(c.Parameters);
                case CommandTypes.Find: return Find(c.Parameters);
                case CommandTypes.Empty: return 0;
                default: { NotImplementedCommandType(); return 1002; }
            }
        }

        private int Connect(Dictionary<string, string> parameters)
        {
            if (db != null && db.IsConnected()) db.Disconnect();
            string dbname = parameters["db"];
            db = new Language(dbname);
            db.Connect();
            return 0;
        }

        private int Build(Dictionary<string, string> parameters)
        {
            string filename = parameters["fromfile"];
            Console.WriteLine($"Начало обучения на файле {filename}");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            db.Create(filename);
            sw.Stop();
            Debug.WriteLine(sw.Elapsed.TotalSeconds + " sec");
            return 0;
        }

        private int ShowHelp()
        {
            foreach (string s in Command.helpstrings)
                Console.WriteLine(s);
            return 0;
        }

        /// <summary>
        /// Поиск слова в БД по параметрам
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private int Find(Dictionary<string, string> parameters)
        {
            if (db == null)
            {
                ErrorMessage("База данных не инициализирована. Сначала загрузите или создайте БД.");
                return 0;
            }
            string text = "";
            int rank = 1;
            int maxcount = 1;
            foreach (string key in parameters.Keys)
                switch (key)
                {
                    case "text": text = parameters[key]; break;
                    case "rank":
                        {
                            if (!int.TryParse(parameters[key], out rank))
                            {
                                IncorrectParameter(key, parameters[key]);
                                return 1001;
                            }; break;
                        }
                    case "top":
                        {
                            if (!int.TryParse(parameters[key], out maxcount))
                            {
                                IncorrectParameter(key, parameters[key]);
                                return 1001;
                            }; break;
                        }
                    default: UnknownParameter(key); break;
                }
            //Вывод информации о команде
            Stopwatch stopwatch = Stopwatch.StartNew();
            DebugMessage($"[{stopwatch.Elapsed.ToString()}] поиск ближайших {maxcount} к '{text}' в лексиконе ранга {rank}");
            List<Term_old> terms = db.Similars(text, rank, maxcount);
            stopwatch.Stop();
            DebugMessage($"[{stopwatch.Elapsed.ToString()}] завершено");
            ShowTerms(terms);
            return 0;
        }

        /// <summary>
        /// Метод исполнения команды создания Языка
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private int Create(Dictionary<string, string> parameters)
        {
            string name = "";
            List<string> splitters = new List<string>();
            foreach (string key in parameters.Keys)
                switch (key)
                {
                    case "name": name = parameters[key]; break;
                    case "splitters": splitters.Add(parameters[key]); break;
                    default: { UnknownParameter(key); return 1001; };
                }
            db = new Language(name, splitters.ToArray());
            return 0;
        }

        //private int AddData(Dictionary<string, string> parameters)
        //{
        //    if (db == null)
        //    {
        //        ErrorMessage("База данных не инициализирована. Сначала загрузите или создайте БД.");
        //        return 0;
        //    }
        //    foreach (string key in parameters.Keys)
        //        switch (key)
        //        {
        //            case "file": return AddDataFromFile(parameters[key]);
        //            case "folder": return AddDataFromFolder(parameters[key]);
        //            case "string": return AddDataFromString(parameters[key]);
        //            default: { UnknownParameter(key); return 1001; };
        //        }
        //    return 0;
        //}

        //private int AddDataFromString(string s)
        //{
        //    Stopwatch stopwatch = Stopwatch.StartNew();
        //    DebugMessage($"[{stopwatch.Elapsed.ToString()}] Добавление данных из строки");
        //    int wordscount = db.CreateFromString(s);
        //    stopwatch.Stop();
        //    Console.WriteLine($"В БД {db.Name} добавлено {wordscount} слов.");
        //    DebugMessage($"[{stopwatch.Elapsed.ToString()}] завершено");
        //    return 0;
        //}

        //private int AddDataFromFolder(string path)
        //{
        //    int wordscount = 0;
        //    Stopwatch stopwatch = Stopwatch.StartNew();
        //    DebugMessage($"[{stopwatch.Elapsed.ToString()}] Добавление данных из папки '{path}'");
        //    string[] files = Directory.GetFiles(path);
        //    foreach (string file in files)
        //        wordscount += db.CreateFromTextFile(file);
        //    Console.WriteLine($"В БД {db.Name} добавлено {wordscount} слов.");
        //    DebugMessage($"[{stopwatch.Elapsed.ToString()}] завершено");
        //    return 0;
        //}

        //private int AddDataFromFile(string filename)
        //{
        //    Stopwatch stopwatch = Stopwatch.StartNew();
        //    DebugMessage($"[{stopwatch.Elapsed.ToString()}] Добавление данных из файла '{filename}'");
        //    int wordscount = db.CreateFromTextFile(filename);
        //    stopwatch.Stop();
        //    Console.WriteLine($"В БД {db.Name} добавлено {wordscount} слов.");
        //    DebugMessage($"[{stopwatch.Elapsed.ToString()}] завершено");
        //    return 0;
        //}

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
            if (ExecuteMode == ShellExecuteMode.Debug)
                Console.WriteLine(s);
        }

        private void ErrorMessage(string s)
        {
            Console.WriteLine(s);
        }

        private void InfoMessage(string s)
        {
            Console.WriteLine(s);
        }

        private void ShowTerms(IEnumerable<Term_old> terms)
        {
            Console.WriteLine();
            terms.ToList().ForEach(term =>
            {
                if (term.id >= 0)
                    Console.WriteLine($"[{term.confidence}] {term.ToString()}");
            });
        }

        private void NotImplementedCommandType()
        {
            Console.WriteLine("Данный тип команды не реализован");
        }
    }
}
