using System;
using System.Linq;

namespace NLDB
{
    //TODO: Создать модульные тесты для Confidence для каждой из метрик (осталось: Inclusive, SoftInclusive, CosineLeft, SoftCosine)
    /// <summary>
    /// Класс, реализующий несколько различных метрик для вычисления схожести Термов
    /// </summary>
    public class Confidence
    {
        public Confidence()
        {
        }

        /// <summary>
        /// Основной метод, вызывающий вычисления метрики, соответствющей рангу Термов a и b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float Compare(Term_old a, Term_old b)
        {
            if (a.rank != b.rank) throw new ArgumentException("Попытка сравнить термы разных рангов");
            return Confidence.Operations[a.rank](a, b);
        }

        /// <summary>
        /// Логическое (двоичное) сравнение двух чисел
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
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
        private static float Inclusive(Term_old a, Term_old b)
        {
            int count = a.Childs.Sum(c => b.Contains(c) ? 1 : 0);
            return count / a.Count;
        }

        /// <summary>
        /// Аналог Inclusive, но для Слов, представленных своими id
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static float Inclusive(int[] a, int[] b)
        {
            int count = a.Sum(c => b.Contains(c) ? 1 : 0);
            return count / a.Length;
        }

        /// <summary>
        /// Метрика схожести, аналогичная Inclusive (считается отношение количества совпадающих подслов к количеству подслов в Терме a),
        /// но также учитывается вес подслов в Слове b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static float SoftInclusive(Term_old a, Term_old b)
        {
            float count = a.Childs.Sum(c => b.Childs.Max(bc => Confidence.Compare(bc, c)));
            return count / a.Count;
        }

        private static float SoftInclusive(int[] a, int[] b)
        {
            float count = a.Sum(c => b.Max(bc => Confidence.Compare(bc, c)));
            return count / a.Length;
        }

        /// <summary>
        /// Косинусная метрика между Термами: сумма совпадающих подслов, делёная на произведение модулей Термов
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static float Cosine(Term_old a, Term_old b)
        {
            int n = a.Count < b.Count ? a.Count : b.Count;
            float s = 0;
            for (int i = 0; i < n; i++)
                s += (a.Childs[i].id == b.Childs[i].id) ? a.Childs[i].confidence : 0;
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

        /// <summary>
        /// Левое косинусная метрика - в отличие от косинусного, в знаменателе не произведение модулей Термов, а модуль терма a
        /// Данная метрика учитывает только модуль (длину) Терма a. Аналогично можно сделать правую косинусную метрику, вызовом CosineLeft(b,a).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static float CosineLeft(Term_old a, Term_old b)
        {
            int n = a.Count < b.Count ? a.Count : b.Count;
            float s = 0;
            for (int i = 0; i < n; i++)
                s += (a.Childs[i].id == b.Childs[i].id) ? a.Childs[i].confidence : 0;
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

        /// <summary>
        /// Мягкая косинусная метрика использует матрицу произведений значений confidence Терма a с собой, Терма b с собой, Терма a с Термом b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static float SoftCosine(Term_old a, Term_old b)
        {
            float s = 0;
            float da = 0, db = 0;
            for (int i = 0; i < a.Count; i++)
                for (int j = 0; j < b.Count; j++)
                    s += a.Childs[i].confidence * b.Childs[j].confidence;
            for (int i = 0; i < a.Count; i++)
                for (int j = 0; j < a.Count; j++)
                    da += a.Childs[i].confidence * a.Childs[j].confidence;
            for (int i = 0; i < b.Count; i++)
                for (int j = 0; j < b.Count; j++)
                    db += b.Childs[i].confidence * b.Childs[j].confidence;
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

        /// <summary>
        /// Метрика бинарного совпадения Термов по идентификаторам
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static float Equality(Term_old a, Term_old b)
        {
            return a.id == b.id ? 1 : 0;
        }

        /// <summary>
        /// Метрика биннарного совпадения Термов по последовательности подслов
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
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
        private static readonly Func<Term_old, Term_old, float>[] Operations = new Func<Term_old, Term_old, float>[5]
        {
            Confidence.Equality,        //для букв
            Confidence.Cosine,          //для слов
            Confidence.SoftInclusive,   //для предложений
            Confidence.SoftInclusive,   //для параграфов
            Confidence.SoftInclusive    //зарезервивровано
        };

    }
}
