using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NLDB.DAL
{
    public class IndexedValue : IEquatable<IndexedValue>,IComparable<IndexedValue>
    {
        public readonly int Index;
        public double Value;

        public IndexedValue(int index, double value)
        {
            Index = index;
            Value = value;
        }

        public override bool Equals(object obj)
        {
            return obj is IndexedValue value &&
                   Index == value.Index &&
                   Value == value.Value;
        }

        public bool Equals(IndexedValue other)
        {
            return other != null &&
                   Index == other.Index &&
                   Value == other.Value;
        }

        public override int GetHashCode()
        {
            var hashCode = 1405935468;
            hashCode = hashCode * -1521134295 + Index.GetHashCode();
            hashCode = hashCode * -1521134295 + Value.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return $"{Index}:{Value}";
        }

        int IComparable<IndexedValue>.CompareTo(IndexedValue other)
        {
            return Math.Sign(Value - other.Value);
        }
    }

    public class ItemIndexComparer : IComparer<IndexedValue>, IEqualityComparer<IndexedValue>
    {
        public int Compare(IndexedValue x, IndexedValue y)
        {
            return x.Index - y.Index;
        }

        public bool Equals(IndexedValue x, IndexedValue y)
        {
            return x.Index - y.Index > 0;
        }

        public int GetHashCode(IndexedValue obj)
        {
            return obj.Index;
        }
    }

    public class SparseVector : IEnumerable<IndexedValue>
    {
        private double? normL1 = null;
        private double? normL2 = null;
        private double? mean = null;

        private List<IndexedValue> values;
        public List<IndexedValue> Values => values;

        public SparseVector(IEnumerable<double> values)
        {
            this.values = values.Select((v, i) => new IndexedValue(i, v)).ToList();
        }

        public SparseVector(IEnumerable<Tuple<int, double>> tuples)
        {
            values = FromTuples(tuples);
        }

        public SparseVector(SparseVector v)
        {
            values = new List<IndexedValue>(v.values);
            normL1 = v.normL1;
            normL2 = v.normL2;
            mean = v.mean;
        }

        public int Count => values.Count;

        public double NormL1()
        {
            if (normL1 == null) normL1 = values.AsParallel().Sum(e => Math.Abs(e.Value));
            return (double)normL1;
        }

        public double NormL2()
        {
            if (normL2 == null) normL2 = Math.Sqrt(values.AsParallel().Sum(e => e.Value * e.Value));
            return (double)normL2;
        }

        public double SquareNormL2()
        {
            return values.AsParallel().Sum(e => e.Value * e.Value);
        }

        public double Mean()
        {
            //Здесь учитываются только не нулевые элементы вектора
            if (mean == null) mean = Sum() / values.Count;
            return (double)mean;
        }

        public double Sum()
        {
            return values.AsParallel().Sum(e => e.Value);
        }

        public double Dispersion()
        {
            return BuildCentered().SquareNormL2();  //Затратно, т.к. строится центрированный вектор
        }

        public double Dot(SparseVector v)
        {
            return this * v;
        }

        public double Correlation(SparseVector v)
        {
            double divisor = NormL2() * v.NormL2();
            if (divisor == 0) return 0; //при нулевой длине одного из векеторов считаем, что корреляция=0
            return (this * v) / divisor;
        }

        public SparseVector BuildCentered()
        {
            double mx = Mean();
            return new SparseVector(values.AsParallel().Select(v => Tuple.Create(v.Index, v.Value - mx)));
        }

        //-------------------------------------------------------------------------------------------------
        // Методы, которые меняют данные вектора
        //-------------------------------------------------------------------------------------------------
        public void Center()
        {
            double mx = Mean();
            values.AsParallel().ForAll(x => x.Value -= mx);
            ResetProperties();
            //mean = 0;
        }

        public void Normalize()
        {
            double d = NormL2();
            if (d == 0)
                values.AsParallel().ForAll(x => x.Value = 0);
            else
                values.AsParallel().ForAll(x => x.Value /= d);
            ResetProperties();
        }

        /// <summary>
        /// Удаляет элементы вектора, значения которых находятся в диапазоне [left, right] включительно.
        /// </summary>
        /// <param name="left">левая граница диапазона</param>
        /// <param name="right">правая граница диапазона</param>
        public int RemoveValuesFromRange(double left, double right)
        {
            int count = values.RemoveAll(e => e.Value >= left && e.Value <= right);
            ResetProperties();
            return count;
        }

        /// <summary>
        /// Доступ к i-му элементу вектора. Время доступа O(log n)
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public double this[int i]
        {
            get
            {
                int index = values.BinarySearch(new IndexedValue(i, 0), new ItemIndexComparer());
                if (index >= 0) return values[i].Value;
                else
                    return 0;
            }
        }

        public IEnumerable<Tuple<int, double>> EnumerateIndexed()
        {
            return values.Select(t => Tuple.Create(t.Index, t.Value));
        }

        public IEnumerator<IndexedValue> GetEnumerator()
        {
            return new SparseVectorEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new SparseVectorEnumerator(this);
        }

        public override string ToString()
        {
            return "[" + string.Join(",", values.AsParallel().Select(v => v.ToString())) + "]";
        }

        //-------------------------------------------------------------------------------------------------
        // Закрытые методы
        //-------------------------------------------------------------------------------------------------
        private void ResetProperties()
        {
            mean = null;
            normL1 = null;
            normL2 = null;
        }
        //-------------------------------------------------------------------------------------------------
        // Статические методы
        //-------------------------------------------------------------------------------------------------
        private static List<IndexedValue> FromTuples(IEnumerable<Tuple<int, double>> tuples)
        {
            return tuples
                .AsParallel()
                .OrderBy(t => t.Item1)
                .Select(t => new IndexedValue(t.Item1, t.Item2))
                .ToList();
        }

        //-------------------------------------------------------------------------------------------------
        // Операторы
        //-------------------------------------------------------------------------------------------------
        public static double operator *(SparseVector vector_a, SparseVector vector_b)
        {
            //vector_a.AsQueryable().Intersect(vector_b, new ElementIndexComparer());
            SparseVectorEnumerator a_enumerator = (SparseVectorEnumerator)vector_a.GetEnumerator();
            SparseVectorEnumerator b_enumerator = (SparseVectorEnumerator)vector_b.GetEnumerator();
            bool end = !(a_enumerator.MoveNext() && b_enumerator.MoveNext());
            double result = 0;
            while (!end)
            {
                IndexedValue a = a_enumerator.Current;
                IndexedValue b = b_enumerator.Current;
                if (a.Index == b.Index)
                {
                    result += a.Value * b.Value;
                    end = !(a_enumerator.MoveNext() && b_enumerator.MoveNext());
                }
                else if (a.Index < b.Index)
                    end = !a_enumerator.SkipTo(b);//a_enumerator.MoveNext();
                else if (a.Index > b.Index)
                    end = !b_enumerator.SkipTo(a);//b_enumerator.MoveNext();
            }
            return result;
        }
    }

    /// <summary>
    /// Перечислитель элементов вектора SparseVector
    /// </summary>
    public class SparseVectorEnumerator : IEnumerator<IndexedValue>, IDisposable
    {
        private SparseVector vector;
        private int currentIndex;
        private IndexedValue current;
        public IndexedValue Current => current;

        object IEnumerator.Current => Current;

        public SparseVectorEnumerator(SparseVector vector)
        {
            this.vector = vector;
            current = null;
            currentIndex = -1;
        }

        public bool MoveNext()
        {
            if (++currentIndex >= vector.Values.Count)
                return false;
            else
                current = vector.Values[currentIndex];
            return true;
        }

        /// <summary>
        /// Метод переводит перечислитель к следующему индексу вектора, совпадающему с индексом item. Если такого нет, переводит 
        /// </summary>
        /// <param name="item">Значение, индекс которого должен совпасть со следующим в случае успеха</param>
        /// <returns></returns>
        public bool SkipTo(IndexedValue item)
        {
            if (currentIndex + 1 >= vector.Values.Count)
                return false;
            int index = vector.Values.BinarySearch(currentIndex + 1, vector.Values.Count - currentIndex - 1, item, new ItemIndexComparer());
            if (index < 0)
                if ((~index) == vector.Count)
                    return false;
                else
                    index = ~index;
            currentIndex = index;
            current = vector.Values[currentIndex];
            return true;
        }

        public void Reset()
        {
            currentIndex = -1;
            current = null;
        }

        void IDisposable.Dispose() { }
    }
}
