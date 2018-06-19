using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    using WordVector = Dictionary<Word, double>;
    public enum CompareFunctionType { Cosine, Injection, OrderedInjection };

    public class Confidence
    {

        public Confidence(Lexicon l)
        {
            this.lexicon = l;
        }

        public void Evaluate(Term a)
        {
            if (a.Rank == 0) { this.EvaluateAtom(a); return; }
            var lex = this.lexicon;
            var sublex = this.lexicon.Child;
            //Выделение претендентов на роль ближайшего
            var parents = a.Childs.
                SelectMany(t => sublex[t.Id].Parents.
                Select(link => lex[link.Id])).
                Distinct().
                Select(p => new Term(p, lex)).
                ToList();

            //Поиск ближайшего родителя, т.е. родителя с максимумом Confedence
            parents.AsParallel().ForAll(p =>
            {
                double confidence = Compare(a, p);
                if (a.Confidence < confidence)
                {
                    a.Id = p.Id;
                    a.Confidence = confidence;
                }
            });
        }

        public IEnumerable<Term> FindMany(Term term, int count = 0)
        {
            List<Term> result = new List<Term>();
            if (term.Rank == 0)
            {
                result.Add(this.FindAtom(term));
                return result;
            }
            var lex = this.lexicon;
            var sublex = this.lexicon.Child;
            //Выделение претендентов на роль ближайшего
            var parents = term.Childs.
                SelectMany(t => sublex[t.Id].Parents.
                Select(link => lex[link.Id])).
                Distinct().
                Select(p => new Term(p, lex)).
                ToList();
            //Расчет оценок Confidence для каждого из соседа
            parents.AsParallel().ForAll(p => p.Confidence = Compare(term, p));
            //Сортировка по убыванию оценки
            parents.Sort(new Comparison<Term>((t1, t2) => (int)(t2.Confidence - t1.Confidence)));
            if (count > 0)
                result.AddRange(parents.Take(count));
            else
                result.AddRange(parents);
            return result;
        }

        public static double Compare(Term a, Term b)
        {
            if (a.Rank != b.Rank) throw new ArgumentException("Попытка сравнить термы разных рангов");
            return Operations[a.Rank](a, b);
        }

        //----------------------------------------------------------------------------------------------------------------
        //Частные методы
        private void EvaluateAtom(Term term)
        {
            int id = this.lexicon.AtomId(term.Text);
            double confidence = (id == -1) ? 0 : 1;
            term.Id = id;
            term.Confidence = confidence;
        }

        private Term FindAtom(Term term)
        {
            Term result = new Term(term);
            this.Evaluate(result);
            return new Term(term);
        }


        //Возвращает 1, если вектор a полностью входит в b
        private static double Inclusive(Term a, Term b)
        {
            int count = a.Childs.Aggregate(0, (c, n) => b.Contains(n) ? c + 1 : c);
            return (count == a.Count) ? 1 : 0;
        }

        private static double SoftInclusive(Term a, Term b)
        {
            double count = a.Childs.Aggregate<Term, double>(0, (c, n) => b.ContainsId(n.Id) ? c + n.Confidence : c);
            return count / a.Count;
        }

        private static double Cosine(Term a, Term b)
        {
            int n = a.Count < b.Count ? a.Count : b.Count;
            double s = 0;
            for (int i = 0; i < n; i++)
                s += (a.Childs[i].Id == b.Childs[i].Id) ? a.Childs[i].Confidence : 0;
            double denominator = Math.Sqrt(a.Count) * Math.Sqrt(b.Count);
            return s / denominator;
        }

        private static double CosineLeft(Term a, Term b)
        {
            int n = a.Count < b.Count ? a.Count : b.Count;
            double s = 0;
            for (int i = 0; i < n; i++)
                s += (a.Childs[i].Id == b.Childs[i].Id) ? a.Childs[i].Confidence : 0;
            return s / a.Count;
        }

        private static double SoftCosine(Term a, Term b)
        {
            double s = 0;
            double da = 0, db = 0;
            for (int i = 0; i < a.Count; i++)
                for (int j = 0; j < b.Count; j++)
                    s += a.Childs[i].Confidence * b.Childs[j].Confidence;
            for (int i = 0; i < a.Count; i++)
                for (int j = 0; j < a.Count; j++)
                    da += a.Childs[i].Confidence * a.Childs[j].Confidence;
            for (int i = 0; i < b.Count; i++)
                for (int j = 0; j < b.Count; j++)
                    db += b.Childs[i].Confidence * b.Childs[j].Confidence;
            return s / (Math.Sqrt(da) * Math.Sqrt(db));
        }

        private static double Equality(Term a, Term b)
        {
            return a.Id == b.Id ? 1 : 0;
        }

        //----------------------------------------------------------------------------------------------------------------
        //Частные свойства и поля
        private readonly Lexicon lexicon;

        //Массив функций для поэлементного вычисления "похожести" векторов слов. Индекс в массиве - ранг сравниваемых слов.
        //Функция применяется к двум скалярным элементам веторов, в соответствующих позициях
        private static readonly Func<Term, Term, double>[] Operations = new Func<Term, Term, double>[4]
        {
            Confidence.Equality,
            Confidence.Cosine,
            Confidence.SoftInclusive,
            Confidence.SoftInclusive
        };

    }
}
