using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    [Serializable]
    public class Term
    {
        public string Text;
        public int Id;
        public int Rank;
        public double Confidence;
        public readonly List<Term> Childs = new List<Term>();
        public readonly Dictionary<Term, int> ChildsBag = new Dictionary<Term, int>();

        public Term(int _id, string _t, List<Term> _childs)
        {
            this.Text = _t;
            this.Id = _id;
            this.Confidence = 0;
            this.Childs = _childs;
            if (this.Childs.Count > 0)
            {
                this.ChildsBag = this.Childs.
                    Distinct().
                    Select((t, i) => new KeyValuePair<Term, int>(t, i)).
                    ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                this.Rank = this.Childs[0].Rank + 1;
            }
            else
                this.Rank = 0;
        }

        public Term(Word w, Lexicon lex)
        {
            this.Text = ""; //для скорости можно было бы lex.ToText(w)
            this.Id = w.Id;
            this.Confidence = 1;
            if (w.Childs.Length > 0)
            {
                this.Childs = w.Childs.Select(
                    c =>
                    (lex.Rank == 0) ? 
                    new Term(c, lex.ToText(c), new List<Term>()) : 
                    new Term(lex.Child[c], lex.Child)).ToList();
                this.ChildsBag = this.Childs.
                    Distinct().
                    Select((t, i) => new KeyValuePair<Term, int>(t, i)).
                    ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                this.Rank = this.Childs[0].Rank + 1;
            }
            else
                this.Rank = 0;
        }

        public bool Contains(Term c)
        {
            return this.ChildsBag.ContainsKey(c);
        }

        public int Count { get { return this.Childs.Count; } }

        public override string ToString() => this.Text;


        public override bool Equals(object obj)
        {
            Term w = (Term)obj;
            //Если указан id, то сравниваем по id (состав может отличаться). Такой способ нужен для поиска с неизвестным составом
            if (this.Id == w.Id) return true;
            //Если длины слов не равны то слова не равны
            if (w.Childs.Count != this.Childs.Count) return false;
            if (w.Childs.Count == 0) return w.Id == this.Id;
            for (int i = 0; i < this.Childs.Count; i++)
                if (w.Childs[i] != this.Childs[i]) return false;
            return true;
        }

        public override int GetHashCode()
        {
            if (this.Childs.Count == 0)
                return this.Id;
            int hash = 0;
            for (int i = 0; i < this.Childs.Count; i++)
            {
                hash += this.Childs[i].Id + 1013904223;
                hash *= 1664525;
            }
            return hash;
        }
    }
}
