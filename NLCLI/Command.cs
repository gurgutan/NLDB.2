/// =========================================================================================================================
/// Слеповичев И.И. 10.2018
/// Модуль с классом Command
/// =========================================================================================================================

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NLDB.NLCLI
{
    public enum CommandTypes { Empty, Help, Create, Build, Connect, Quit, Find };

    /// <summary>
    /// Класс для создания команды. На данный момент (08.10.2018) команда является пассивным набором данных, необходимым для выполнения действий.
    /// Планируется добавить средства исполнения команды и управления последовательностью команд.
    /// </summary>
    public class Command
    {
        public static readonly List<string> ptrns = new List<string>
        {
            @"^(?'CMD'create)\s+(?'NAMEKEY'name):(?'NAMEVAL'[\w]+)\s+(?'SPLKEY'splitters):(?'SPLVAL'"".+""\,?)+",
            @"^(?'CMD'build)\s+(?'LEX'fromfile):(?'SRC'.+)",
            @"^(?'CMD'connect)\s+(?'TO'db):(?'SRC'.+)",
            @"^(?'CMD'save)\s+(?'TO'(file|folder|string)):(?'SRC'.+)",
            @"^(?'CMD'find)\s+((?'RANK'rank):(?'RANKVAL'\d{1,4})\s+)?((?'TOP'top):(?'TOPVAL'\d{1,15})\s+)?((?'TEXT'text):(?'TEXTVAL'.+))",
            @"^(?'CMD'clear)",
            @"^(?'CMD'help)",
            @"^(?'CMD'quit)"
        };

        public static readonly List<string> helpstrings = new List<string>
        {
            @"create name:<name> splitters:""<split_expr1>"" ""<split_expr2>"" ""<split_exprN>""",
            @"build fromfile:<path>",
            @"connect db:<path>",
            //@"save file:<path>",
            @"find [rank:<r>] [top:<n>] text:<string>",
            //@"clear",
            //@"help",
            @"quit"
        };

        public CommandTypes CommandType = CommandTypes.Empty;
        public Dictionary<string, string> Parameters = new Dictionary<string, string>();
        public string Result;

        public bool TryParse(string line)
        {
            Parameters.Clear();
            line = line.ToLower();
            foreach (string ptrn in ptrns)
            {
                Match match = Regex.Match(line, ptrn, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                if (!match.Success) continue;
                string cmdType = match.Groups[1].Value;
                switch (cmdType)
                {
                    case "create":
                        {
                            CommandType = CommandTypes.Create;
                            Parameters.Add(match.Groups[2].Value, match.Groups[3].Value);
                            Parameters.Add(match.Groups[4].Value, match.Groups[5].Value);
                            return true;
                        };
                    case "build":
                        {
                            CommandType = CommandTypes.Build;
                            Parameters.Add(match.Groups[2].Value, match.Groups[3].Value);
                            return true;
                        };
                    case "connect":
                        {
                            CommandType = CommandTypes.Connect;
                            Parameters.Add(match.Groups[2].Value, match.Groups[3].Value);
                            return true;
                        };
                    case "quit":
                        {
                            CommandType = CommandTypes.Quit;
                            return true;
                        };
                    case "find":
                        {
                            CommandType = CommandTypes.Find;
                            for (int i = 2; i < match.Groups.Count; i += 2)
                                Parameters.Add(match.Groups[i].Value, match.Groups[i + 1].Value);
                            return true;
                        };
                    case "help":
                        {
                            CommandType = CommandTypes.Help;
                            return true;
                        }
                    default:
                        {
                            Console.WriteLine("Неизвестная команда: " + cmdType);
                            return false;
                        }
                }
            }
            return false;
        }

    }
}
