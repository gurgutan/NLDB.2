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

        public Confidence(Lexicon l)
        {
            this.lexicon = l;
        }

        public void Evaluate(Term term)
        {
            if (term.Rank == 0)
            {
                int id = this.lexicon.AtomId(term.Text);
                term.Id = id;
                term.Confidence = (id == -1) ? 0 : 1;
                return;
            }

            Dictionary<Word, double> confidences = new Dictionary<Word, double>();
            //Значение в знаменателе соответствует квадрату длины терма
            for (int i = 0; i < term.Childs.Count; i++)
            {
                var subterm = term.Childs[i];
                if (subterm.Id == -1) throw new ArgumentException("Невозможно определить слово для терма " + subterm.Text);
                Word subword = this.lexicon.Child[subterm.Id];
                var links = subword.Parents;
                foreach (var link in links)
                {
                    Word word = this.lexicon[link.id];
                    //С учетом порядка букв в слове
                    if (!confidences.ContainsKey(word))
                        confidences[word] = 0;
                    switch (term.Rank)
                    {
                        case 1: confidences[word] += CosSimilarityIncrement(i, link.pos) * subterm.Confidence; break;
                        case 2: confidences[word] += ConstSimilarityIncrement(i, link.pos) * subterm.Confidence; break;
                        case 3: confidences[word] += ConstSimilarityIncrement(i, link.pos) * subterm.Confidence; break;
                        default: throw new ArgumentOutOfRangeException("Для слов ранга >3 вычисление расстояния не реализовано");
                    }
                }
            }
            double t_size = term.Childs.Count;
            foreach (var d in confidences)
            {
                double w_size = d.Key.Childs.Length;
                //cos(a,b) = (a*b)/(|a|*|b|)
                //В нашем случае m=max(длина_терма, длина_слова),
                //следовательно |a|*|b|=Sqrt(<a,a>)*Sqrt(<b,b>)=Sqrt(<m,m>)*Sqrt(<m,m>)=m,                 
                double denominator = Math.Max(w_size, t_size);
                double value = d.Value / denominator;
                if (value > term.Confidence)
                {
                    term.Confidence = value;
                    term.Id = d.Key.Id;
                }
            }
        }

        //private void Evaluate2(Term term)
        //{
        //    Dictionary<Word, double> confidences = new Dictionary<Word, double>();
        //    //Значение в знаменателе соответствует квадрату длины терма
        //    for (int i = 0; i < term.Childs.Count; i++)
        //    {
        //        var subterm = term.Childs[i];
        //        if (subterm.Id == -1) throw new ArgumentException("Невозможно определить слово для терма " + subterm.Text);
        //        Word subword = this.lexicon.Child[subterm.Id];
        //        var links = subword.Parents;
        //        foreach (var link in links)
        //        {
        //            Word word = this.lexicon[link.id];
        //            //Без учета порядка слов в предложении
        //            if (!confidences.ContainsKey(word))
        //                confidences[word] = 1 * subterm.Confidence;
        //            else
        //                confidences[word] += 1 * subterm.Confidence;
        //        }
        //    }
        //    double t_size = term.Childs.Count;
        //    foreach (var d in confidences)
        //    {
        //        double w_size = d.Key.Childs.Length;
        //        //cos(a,b) = (a*b)/(|a|*|b|)
        //        //В нашем случае m=max(длина_терма, длина_слова),
        //        //следовательно |a|*|b|=Sqrt(<a,a>)*Sqrt(<b,b>)=Sqrt(<m,m>)*Sqrt(<m,m>)=m,                 
        //        double denominator = Math.Max(w_size, t_size);
        //        double value = d.Value / denominator;
        //        if (value > term.Confidence)
        //        {
        //            term.Confidence = value;
        //            term.Id = d.Key.Id;
        //        }
        //    }
        //}

        //private void Evaluate3(Term term)
        //{
        //    Dictionary<Word, double> confidences = new Dictionary<Word, double>();
        //    //Значение в знаменателе соответствует квадрату длины терма
        //    for (int i = 0; i < term.Childs.Count; i++)
        //    {
        //        var subterm = term.Childs[i];
        //        if (subterm.Id == -1) throw new ArgumentException("Невозможно определить слово для терма " + subterm.Text);
        //        Word subword = this.lexicon.Child[subterm.Id];
        //        var links = subword.Parents;
        //        foreach (var link in links)
        //        {
        //            Word word = this.lexicon[link.id];
        //            //Без учета порядка слов в предложении
        //            if (!confidences.ContainsKey(word))
        //                confidences[word] = 1 * subterm.Confidence;
        //            else
        //                confidences[word] += 1 * subterm.Confidence;
        //        }
        //    }
        //    double t_size = term.Childs.Count;
        //    foreach (var d in confidences)
        //    {
        //        double w_size = d.Key.Childs.Length;
        //        //cos(a,b) = (a*b)/(|a|*|b|)
        //        //В нашем случае m=max(длина_терма, длина_слова),
        //        //следовательно |a|*|b|=Sqrt(<a,a>)*Sqrt(<b,b>)=Sqrt(<m,m>)*Sqrt(<m,m>)=m,                 
        //        double denominator = Math.Max(w_size, t_size);
        //        double value = d.Value / denominator;
        //        if (value > term.Confidence)
        //        {
        //            term.Confidence = value;
        //            term.Id = d.Key.Id;
        //        }
        //    }
        //}

        private double CosSimilarityIncrement(double a, double b)
        {
            return AreEqual(a, b);
        }

        private double ConstSimilarityIncrement(double a, double b)
        {
            return 1;
        }

        private double AreEqual(double i, double j)
        {
            return (i == j) ? 1 : 0;
        }

    }
}
