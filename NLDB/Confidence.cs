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

        public static float Compare(Term a, Term b)
        {
            if (a.rank != b.rank) throw new ArgumentException("Попытка сравнить термы разных рангов");
            return Confidence.Operations[a.rank](a, b);
        }

        public static float Compare(int a, int b)
        {
            return a == b ? 1 : 0;
        }

        /// <summary>
        /// Возвращает отношение количества дочерних слов, вошедших в b и количетсва дочерних слов b. Учитываются точные вхождения
        /// </summary>
        /// <param name="a">терм</param>
        /// <param name="b">терм</param>
        /// <returns>оценка схожести из интервала [0,1], где 0 - не похожи, 1 - максимально похожи</returns>
        private static float Inclusive(Term a, Term b)
        {
            int count = a.childs.Sum(c => b.Contains(c) ? 1 : 0);
            return count / a.Count;
        }

        private static float Inclusive(int[] a, int[] b)
        {
            int count = a.Sum(c => b.Contains(c) ? 1 : 0);
            return count / a.Length;
        }

        private static float SoftInclusive(Term a, Term b)
        {
            float count = a.childs.Sum(c => b.childs.Max(bc => Confidence.Compare(bc, c)));
            return count / a.Count;
        }

        private static float SoftInclusive(int[] a, int[] b)
        {
            float count = a.Sum(c => b.Max(bc => Confidence.Compare(bc, c)));
            return count / a.Length;
        }

        private static float Cosine(Term a, Term b)
        {
            int n = a.Count < b.Count ? a.Count : b.Count;
            float s = 0;
            for (int i = 0; i < n; i++)
                s += (a.childs[i].id == b.childs[i].id) ? a.childs[i].confidence : 0;
            float denominator = (float)(Math.Sqrt(a.Count) * Math.Sqrt(b.Count));
            return s / denominator;
        }

        private static float Cosine(int[] a, int[] b)
        {
            int n = a.Length < b.Length ? a.Length : b.Length;
            float s = 0;
            for (int i = 0; i < n; i++)
                s += (a[i] == b[i]) ? 1 : 0;
            float denominator = (float)(Math.Sqrt(a.Length) * Math.Sqrt(b.Length));
            return s / denominator;
        }

        private static float CosineLeft(Term a, Term b)
        {
            int n = a.Count < b.Count ? a.Count : b.Count;
            float s = 0;
            for (int i = 0; i < n; i++)
                s += (a.childs[i].id == b.childs[i].id) ? a.childs[i].confidence : 0;
            return s / a.Count;
        }

        private static float CosineLeft(int[] a, int[] b)
        {
            int n = a.Length < b.Length ? a.Length : b.Length;
            float s = 0;
            for (int i = 0; i < n; i++)
                s += (a[i] == b[i]) ? 1 : 0;
            return s / a.Length;
        }

        private static float SoftCosine(Term a, Term b)
        {
            float s = 0;
            float da = 0, db = 0;
            for (int i = 0; i < a.Count; i++)
                for (int j = 0; j < b.Count; j++)
                    s += a.childs[i].confidence * b.childs[j].confidence;
            for (int i = 0; i < a.Count; i++)
                for (int j = 0; j < a.Count; j++)
                    da += a.childs[i].confidence * a.childs[j].confidence;
            for (int i = 0; i < b.Count; i++)
                for (int j = 0; j < b.Count; j++)
                    db += b.childs[i].confidence * b.childs[j].confidence;
            return (float)(s / (Math.Sqrt(da) * Math.Sqrt(db)));
        }

        private static float SoftCosine(int[] a, int[] b)
        {
            float s = 0;
            float da = 0, db = 0;
            for (int i = 0; i < a.Length; i++)
                for (int j = 0; j < b.Length; j++)
                    s += Equality(a[i], b[j]);
            for (int i = 0; i < a.Length; i++)
                for (int j = 0; j < a.Length; j++)
                    da += Equality(a[i], a[j]);
            for (int i = 0; i < b.Length; i++)
                for (int j = 0; j < b.Length; j++)
                    db += Equality(b[i], b[j]);
            return (float)(s / (Math.Sqrt(da) * Math.Sqrt(db)));
        }

        private static float Equality(Term a, Term b)
        {
            return a.id == b.id ? 1 : 0;
        }

        private static float Equality(int[] a, int[] b)
        {
            return a.SequenceEqual(b) ? 1 : 0;
        }

        private static float Equality(int a, int b)
        {
            return a == b ? 1 : 0;
        }

        //----------------------------------------------------------------------------------------------------------------
        //Частные свойства и поля
        //----------------------------------------------------------------------------------------------------------------
        //Массив функций для поэлементного вычисления "похожести" векторов слов. Индекс в массиве - ранг сравниваемых слов.
        //Функция применяется к двум скалярным элементам веторов, в соответствующих позициях
        private static readonly Func<Term, Term, float>[] Operations = new Func<Term, Term, float>[5]
        {
            Confidence.Equality,
            Confidence.Cosine,
            Confidence.SoftInclusive,
            Confidence.SoftInclusive,
            Confidence.SoftInclusive
        };

    }
}
