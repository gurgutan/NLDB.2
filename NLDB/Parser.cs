using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NLDB
{
    public class Parser
    {
        private string SplitExpr;
        //private string RemoveExpr = "";
        private readonly Regex splitRegex;
        private readonly Regex removeRegex;

        public Parser(string splitExpr)
        {
            this.SplitExpr = splitExpr;
            this.splitRegex = new Regex(this.SplitExpr, RegexOptions.Compiled);
            this.removeRegex = new Regex(@"[^а-яА-ЯёЁ\s\t\r\n\!\?\,\;\:\*\+\-\&\^\(\)\[\]\{\}\=\<\>\""\']", RegexOptions.Compiled);
        }

        public string[] Split(string text)
        {
            return this.splitRegex.Split(text);
        }

        public string Normilize(string text)
        {
            string formated = text.ToLower().Trim();
            return this.removeRegex.Replace(formated, "");
        }
    }
}
