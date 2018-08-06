using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public class Term : IComparable
    {
        public int rank;
        public int id;
        public float confidence;
        public string text;

        private List<Term> childs;
        public List<Term> Childs { get { return childs; } }

        private List<Term> parents;
        public List<Term> Parents { get { return parents; } }

        public bool Evaluated { get; set; }

        public Term(int _rank, int _id, float _confidence, string _text, IEnumerable<Term> _childs = null, IEnumerable<Term> _parents = null)
        {
            Evaluated = false;
            rank = _rank;
            id = _id;
            confidence = _confidence;
            text = _text;
            if (_childs != null)
                childs = new List<Term>(_childs);
            if (_parents != null)
                parents = new List<Term>(_parents);
        }

        public override string ToString()
        {
            if (rank == 0)
                return text;
            else
                return "{" + childs.Aggregate("", (c, n) => c == "" ? n.ToString() : c + "" + n.ToString()) + "}";
        }

        public bool Contains(Term t)
        {
            return childs.Any(c => c.id == t.id);
        }

        public int Count { get { return childs == null ? 0 : childs.Count; } }

        public override bool Equals(object obj)
        {
            Term t = (Term)obj;
            //Если указан id, то сравниваем по id (состав может отличаться). Такой способ нужен для поиска с неизвестным составом
            if (this.id == t.id) return true;
            //Если один из термов не имеет дочерних слов (и id не равны), то термы не равны
            if (t.childs == null || this.childs == null || t.Count == 0 || this.Count == 0) return false;
            //Если длины слов не равны то слова не равны
            if (t.Count != this.Count) return false;
            //Если тривиальные случаи не сработали, то сравниваем каждое дочернее слово с соответствующим
            for (int i = 0; i < this.Count; i++)
                if (t.childs[i] != this.childs[i]) return false;
            return true;
        }

        /// <summary>
        /// Хэш-код терма зависит от ранга rank, id, childs 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if (this.childs == null || this.Count == 0)
                return this.id;
            int hash = rank * 1664525;
            for (int i = 0; i < this.Count; i++)
            {
                hash += this.childs[i].id + 1013904223;
                hash *= 1664525;
            }
            return hash;
        }

        /// <summary>
        /// Сравнение по confidence
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            return confidence.CompareTo(obj);
        }

    }

    public class TermComparer : IEqualityComparer<Term>
    {
        public bool Equals(Term x, Term y)
        {
            return x.id == y.id;
        }

        public int GetHashCode(Term obj)
        {
            return obj.GetHashCode();
        }
    }
}
