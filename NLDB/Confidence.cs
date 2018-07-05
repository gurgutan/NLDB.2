using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    //TODO: Создать модульные тесты для Confidence
    public class Confidence
    {
        public Confidence()
        {
        }

        public static double Compare(Term a, Term b)
        {
            if (a.rank != b.rank) throw new ArgumentException("Попытка сравнить термы разных рангов");
            return Confidence.Operations[a.rank](a, b);
        }

        /// <summary>
        /// Возвращает отношение количества дочерних слов, вошедших в b и количетсва дочерних слов b. Учитываются точные вхождения
        /// </summary>
        /// <param name="a">терм</param>
        /// <param name="b">терм</param>
        /// <returns>оценка схожести из интервала [0,1], где 0 - не похожи, 1 - максимально похожи</returns>
        private static double Inclusive(Term a, Term b)
        {
            int count = a.childs.Sum(c => b.Contains(c) ? 1 : 0);
            return count / a.Count;
        }

        private static double SoftInclusive(Term a, Term b)
        {
            double count = a.childs.Sum(c => b.Contains(c) ? c.confidence : 0);
            return count / a.Count;
        }

        private static double Cosine(Term a, Term b)
        {
            int n = a.Count < b.Count ? a.Count : b.Count;
            double s = 0;
            for (int i = 0; i < n; i++)
                s += (a.childs[i].id == b.childs[i].id) ? a.childs[i].confidence : 0;
            double denominator = Math.Sqrt(a.Count) * Math.Sqrt(b.Count);
            return s / denominator;
        }

        private static double CosineLeft(Term a, Term b)
        {
            int n = a.Count < b.Count ? a.Count : b.Count;
            double s = 0;
            for (int i = 0; i < n; i++)
                s += (a.childs[i].id == b.childs[i].id) ? a.childs[i].confidence : 0;
            return s / a.Count;
        }

        private static double SoftCosine(Term a, Term b)
        {
            double s = 0;
            double da = 0, db = 0;
            for (int i = 0; i < a.Count; i++)
                for (int j = 0; j < b.Count; j++)
                    s += a.childs[i].confidence * b.childs[j].confidence;
            for (int i = 0; i < a.Count; i++)
                for (int j = 0; j < a.Count; j++)
                    da += a.childs[i].confidence * a.childs[j].confidence;
            for (int i = 0; i < b.Count; i++)
                for (int j = 0; j < b.Count; j++)
                    db += b.childs[i].confidence * b.childs[j].confidence;
            return s / (Math.Sqrt(da) * Math.Sqrt(db));
        }

        private static double Equality(Term a, Term b)
        {
            return a.id == b.id ? 1 : 0;
        }

        //----------------------------------------------------------------------------------------------------------------
        //Частные свойства и поля
        //----------------------------------------------------------------------------------------------------------------
        //Массив функций для поэлементного вычисления "похожести" векторов слов. Индекс в массиве - ранг сравниваемых слов.
        //Функция применяется к двум скалярным элементам веторов, в соответствующих позициях
        private static readonly Func<Term, Term, double>[] Operations = new Func<Term, Term, double>[5]
        {
            Confidence.Equality,
            Confidence.Cosine,
            Confidence.SoftInclusive,
            Confidence.SoftInclusive,
            Confidence.SoftInclusive
        };

    }
}
