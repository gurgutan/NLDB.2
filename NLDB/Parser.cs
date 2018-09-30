using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
        const string specSymbolNumber = "{число}";
        const string specSymbolRomeNumber = "{число}";
        const string specSymbolEnglish = "{англяз}";
        const string specSymbolShorthand = "{сокращение}";

        private readonly string SplitExpr;
        //private string RemoveExpr = "";
        private readonly Regex splitRegex;
        private readonly Regex removeRegex;
        private readonly Regex replaceNumbersRegex;
        private readonly Regex replaceEnglishRegex;
        private readonly Regex removeShorthands;

        public Parser(string splitExpr)
        {
            this.SplitExpr = splitExpr;
            this.splitRegex = new Regex(this.SplitExpr, RegexOptions.Compiled);
            this.removeRegex = new Regex(@"[^а-яА-ЯёЁ\d\s\n\!\?\.\,\;\:\*\+\-\&\\\/\%\$\^\(\)\[\]\{\}\=\<\>\""\']", RegexOptions.Compiled);
            this.replaceNumbersRegex = new Regex(@"\b\d+((\.|\,)\d+)?", RegexOptions.Compiled);
            this.replaceEnglishRegex = new Regex(@"[a-zA-Z]+", RegexOptions.Compiled);
            this.removeShorthands = new Regex(@"\b([а-яА-ЯёЁ]\s\.)", RegexOptions.Compiled);
        }

        public string[] Split(string text)
        {
            return this.splitRegex.Split(text);
        }

        public string Normilize(string text)
        {
            text = removeRegex.Replace(text.ToLower().Trim(), "");
            text = replaceNumbersRegex.Replace(text, specSymbolNumber);
            text = replaceEnglishRegex.Replace(text, specSymbolEnglish);
            //text = removeShorthands.Replace(text, specSymbolShorthand);
            return text;
        }

    }
}
