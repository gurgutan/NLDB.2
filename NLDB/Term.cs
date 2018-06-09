using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public class Term
    {
        public string Text;
        public int Id;
        public int Rank;
        public double Confidence;
        public List<Term> Childs = new List<Term>();

        public Term(string _t, List<Term> _childs)
        {
            this.Text = _t;
            this.Id = -1;
            this.Confidence = 0;
            this.Childs = _childs;
            if (this.Childs.Count > 0)
                this.Rank = this.Childs[0].Rank + 1;
            else
                this.Rank = 0;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
