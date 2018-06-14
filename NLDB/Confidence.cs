﻿using System;
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

        public void Evaluate(Term term)
        {
            switch (term.Rank)
            {
                case 0: this.EvaluateNearestToAtom(term); break;
                case 1: this.EvaluateNearest(term); break;
                case 2: this.EvaluateNearest(term); break;
                case 3: this.EvaluateNearest(term); break;
                default: throw new ArgumentOutOfRangeException("Для терма ранга >3 определение ближайшего не определено");
            }
        }

        public void EvaluateNearest(Term a)
        {
            var lex = this.lexicon;
            var sublex = this.lexicon.Child;
            if (a.Rank == 0)
            {
                this.EvaluateNearestToAtom(a);
                return;
            }
            //Выделение претендентов на роль ближайшего
            var parents = a.Childs.SelectMany(t => sublex[t.Id].Parents.
                Select(link => lex[link.Id])).
                Distinct().
                Select(p => new Term(p, sublex));
            //Поиск ближайшего
            var nearest = parents.Aggregate<Term, Term>(null,
                (c, n) =>
                {
                    if (c == null) return n;
                    if (c.Confidence < this.Compare(a, n)) return n;
                    return c;
                });
            a = nearest;
            //Dictionary<Word, double> confidences = new Dictionary<Word, double>();
            ////Получаем список подслов, входящих в терм term.Childs
            //var subwords = term.Childs.Select(t => this.lexicon.Child[t.Id]).ToList();
            ////Вычисляем оценки всех возможных вариантов соответствия терму term
            //for (int i = 0; i < subwords.Count; i++)
            //{
            //    double k = term.Childs[i].Confidence;
            //    subwords[i].Parents.ForEach(link =>
            //    {
            //        Word p = this.lexicon[link.Id];
            //        if (!confidences.ContainsKey(p)) confidences.Add(p, 0);
            //        confidences[p] += this.PointwiseOperations[term.Rank](i, link.pos) * k;
            //    });
            //}
            ////Вычисляем максимум confidence как cos(терм, слово) по всем словам Лексикона
            ////cos(a,b) = (a*b)/(|a|*|b|)
            ////В нашем случае m=max(длина_терма, длина_слова),
            ////следовательно |a|*|b|=Sqrt(<a,a>)*Sqrt(<b,b>)=Sqrt(<m,m>)*Sqrt(<m,m>)=m           
            //confidences.Aggregate(term, (c, n) =>
            //{
            //    double value = n.Value / Math.Max(n.Key.Childs.Length, term.Childs.Count);
            //    if (value > c.Confidence)
            //    {
            //        c.Id = n.Key.Id;
            //        c.Confidence = value;
            //        return c;
            //    }
            //    else
            //        return c;
            //});
        }

        public double Compare(Term a, Term b)
        {
            if (a.Rank != b.Rank) throw new ArgumentException("Попытка сравнить термы разных рангов");
            return 
        }

        //Возвращает 1, если вектор a полностью входит в b
        private static double Inclusive(Term a, Term b)
        {
            int count = a.Childs.Aggregate(0, (c, n) => b.Contains(n) ? c + 1 : c);
            return (count == a.Count) ? 1 : 0;
        }

        private static double SoftInclusive(Term a, Term b)
        {
            double count = a.Childs.Aggregate<Term, double>(0, (c, n) => b.Contains(n) ? c + n.Confidence : c);
            return count / a.Count;
        }

        private static double Cosine(Term a, Term b)
        {
            int n = Math.Min(a.Count, b.Count);
            double s = 0;
            for (int i = 0; i < n; n++)
                s += (a.Childs[i].Id == b.Childs[i].Id) ? a.Childs[i].Confidence : 0;
            double denominator = a.Count * b.Count;
            return s / denominator;
        }

        private static double SoftCosine(Term a, Term b)
        {
            double s = 0;
            double da = 0, db = 0;
            for (int i = 0; i < a.Count; i++)
                for (int j = 0; j < b.Count; j++)
                    s += this.Compare(a.Childs[i], b.Childs[j]);
            for (int i = 0; i < a.Count; i++)
                for (int j = 0; j < a.Count; j++)
                    da += this.Compare(a.Childs[i], a.Childs[j]);
            for (int i = 0; i < b.Count; i++)
                for (int j = 0; j < b.Count; j++)
                    db += this.Compare(b.Childs[i], b.Childs[j]);
            return s / (Math.Sqrt(da) * Math.Sqrt(db));
        }

        private static double Equality(Term a, Term b)
        {
            return a.Id == b.Id ? 1 : 0;
        }

        private void EvaluateNearestToAtom(Term term)
        {
            term.Id = this.lexicon.AtomId(term.Text);
            term.Confidence = (term.Id == -1) ? 0 : 1;
            return;
        }

        //----------------------------------------------------------------------------------------------------------------
        //Частные свойства и поля
        private readonly Lexicon lexicon;
        //Массив функций для поэлементного вычисления "похожести" векторов слов. Индекс в массиве - ранг сравниваемых слов.
        //Функция применяется к двум скалярным элементам веторов, в соответствующих позициях
        private readonly Func<Term, Term, double>[] Operations = new Func<Term, Term, double>[4]
        {
            Confidence.Equality,
            Confidence.Cosine,
            Confidence.SoftCosine,
            Confidence.ConstOne
        };

    }
}
