using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NLDB
{
    //TODO: Создать модульные тесты для Parser
    [Serializable]
    public class Parser
    {
        const string specSymbolNumber = "{ч}";
        const string specSymbolRomeNumber = "{рч}";
        const string specSymbolEnglish = "{а}";
        private readonly string SplitExpr;
        //private string RemoveExpr = "";
        private readonly Regex splitRegex;
        private readonly Regex removeRegex;
        private readonly Regex replaceNumbersRegex;
        private readonly Regex replaceEnglishRegex;

        public Parser(string splitExpr)
        {
            this.SplitExpr = splitExpr;
            this.splitRegex = new Regex(this.SplitExpr, RegexOptions.Compiled);
            this.removeRegex = new Regex(@"[^а-яА-ЯёЁa-z\d\s\n\!\?\.\,\;\:\*\+\-\&\\\/\%\$\^\(\)\[\]\{\}\=\<\>\""\']", RegexOptions.Compiled);
            this.replaceNumbersRegex = new Regex(@"\b\d+((\.|\,)\d+)?");
            this.replaceEnglishRegex = new Regex(@"[a-zA-Z]+");
        }

        public string[] Split(string text)
        {
            return this.splitRegex.Split(text);
        }

        public string Normilize(string text)
        {
            text = text.ToLower().Trim();
            text = removeRegex.Replace(text, "");
            //text = replaceNumbersRegex.Replace(text, specSymbolNumber);
            //text = replaceEnglishRegex.Replace(text, specSymbolEnglish);
            return text;
        }

    }
}
