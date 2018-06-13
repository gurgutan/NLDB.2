using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public class Confidence
    {
        private readonly Lexicon lexicon;
        //Массив функций для поэлементного вычисления "похожести" векторов слов. Индекс в массиве - ранг сравниваемых слов.
        //Функция применяется к двум скалярным элементам веторов, в соответствующих позициях
        private readonly Func<double, double, double>[] PointwiseOperations = new Func<double, double, double>[4]
        {
            Confidence.DotMul,
            Confidence.DotMul,
            Confidence.ConstOne,
            Confidence.ConstOne
        };

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

        public void EvaluateNearest(Term term)
        {
            Dictionary<Word, double> confidences = new Dictionary<Word, double>();
            //Получаем список подслов, входящих в терм term.Childs
            var subwords = term.Childs.Select(t => this.lexicon.Child[t.Id]).ToList();
            //Вычисляем оценки всех возможных вариантов соответствия терму term
            for (int i = 0; i < subwords.Count; i++)
            {
                double k = term.Childs[i].Confidence;
                subwords[i].Parents.ForEach(link =>
                {
                    Word p = this.lexicon[link.Id];
                    if (!confidences.ContainsKey(p)) confidences.Add(p, 0);
                    confidences[p] += this.PointwiseOperations[term.Rank](i, link.pos) * k;
                });
            }
            //Вычисляем максимум confidence
            confidences.Aggregate(term, (c, n) =>
            {
                double value = n.Value / Math.Max(n.Key.Childs.Length, term.Childs.Count);
                if (value > c.Confidence)
                {
                    c.Id = n.Key.Id;
                    c.Confidence = value;
                    return c;
                }
                else
                    return c;
            });

            //for (int i = 0; i < term.Childs.Count; i++)
            //{
            //    var subterm = term.Childs[i];
            //    if (subterm.Id == -1) throw new ArgumentException("Невозможно определить слово для терма " + subterm.Text);
            //    Word subword = this.lexicon.Child[subterm.Id];
            //    var links = subword.Parents;
            //    foreach (var link in links)
            //    {
            //        Word word = this.lexicon[link.Id];
            //        //С учетом порядка букв в слове
            //        if (!confidences.ContainsKey(word)) confidences[word] = 0;
            //        switch (term.Rank)
            //        {
            //            case 1: confidences[word] += DotMul(i, link.pos) * subterm.Confidence; break;
            //            case 2: confidences[word] += ConstOne(i, link.pos) * subterm.Confidence; break;
            //            case 3: confidences[word] += ConstOne(i, link.pos) * subterm.Confidence; break;
            //            default: throw new ArgumentOutOfRangeException("Для слов ранга >3 вычисление расстояния не реализовано");
            //        }
            //    }
            //}
            //double t_size = term.Childs.Count;
            //foreach (var d in confidences)
            //{
            //    double w_size = d.Key.Childs.Length;
            //    //cos(a,b) = (a*b)/(|a|*|b|)
            //    //В нашем случае m=max(длина_терма, длина_слова),
            //    //следовательно |a|*|b|=Sqrt(<a,a>)*Sqrt(<b,b>)=Sqrt(<m,m>)*Sqrt(<m,m>)=m,                 
            //    double denominator = Math.Max(w_size, t_size);
            //    double value = d.Value / denominator;
            //    if (value > term.Confidence)
            //    {
            //        term.Confidence = value;
            //        term.Id = d.Key.Id;
            //    }
            //}
        }

        private void EvaluateNearestToAtom(Term term)
        {
            term.Id = this.lexicon.AtomId(term.Text);
            term.Confidence = (term.Id == -1) ? 0 : 1;
            return;
        }

        private static double DotMul(double a, double b)
        {
            return (a == b) ? 1 : 0;
        }

        private static double ConstOne(double a, double b)
        {
            return 1;
        }

    }
}
