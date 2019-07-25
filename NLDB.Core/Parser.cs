using System;
using System.Text.RegularExpressions;

namespace NLDB
{
    //TODO: Создать модульные тесты для Parser
    /// <summary>
    /// Класс, используемый для иерархической токенизации текста.
    /// Иерархическая токенизация - такое разбиение текста на врагменты, в котором каждый фрагмент имеет свой идентификатор и,
    /// либо состоит из других фрагментов, либо является неделимым фрагментом текста (символом)
    /// </summary>
    [Serializable]
    public class Parser
    {
        //Служебные константы, используемые для подстановки в текст для сокращения общего количества токенов по тексту
        private const string specSymbolNumber = "{ХХХ}";
        private const string specSymbolRomeNumber = "{ХХХ}";
        private const string specSymbolEnglish = "{англ}";
        private const string specSymbolShorthand = "";

        private readonly string SplitExpr;
        //private string RemoveExpr = "";
        private readonly Regex splitRegex;
        private static Regex removeRegex = new Regex(@"[^а-яА-ЯёЁ\d\s\n\!\.\,\;\:\-\{\}\=\<\>\""\p{Pd}]", RegexOptions.Compiled);
        private static Regex replaceNumbersRegex = new Regex(@"\b\d+((\.|\,)\d+)?", RegexOptions.Compiled);
        private static Regex replaceEnglishRegex = new Regex(@"[a-zA-Z]+", RegexOptions.Compiled);
        private static Regex removeShorthands = new Regex(@"\b[а-яА-ЯёЁ]\s?\.", RegexOptions.Compiled);

        public Parser(string splitExpr)
        {
            SplitExpr = splitExpr;
            splitRegex = new Regex(SplitExpr, RegexOptions.Compiled);
        }

        public Parser()
        {
            SplitExpr = "";
            splitRegex = new Regex(SplitExpr, RegexOptions.Compiled);
        }

        public string[] Split(string text)
        {
            return splitRegex.Split(text);
        }

        public static string Normilize(string text)
        {
            text = removeRegex.Replace(text.ToLower().Trim(), "");
            text = removeShorthands.Replace(text, specSymbolShorthand);
            text = replaceNumbersRegex.Replace(text, specSymbolNumber);
            text = replaceEnglishRegex.Replace(text, specSymbolEnglish);
            return text;
        }

    }
}
