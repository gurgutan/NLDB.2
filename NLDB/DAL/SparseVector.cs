using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NLDB.DAL
{
    public class IndexedValue
    {
        public readonly int Index;
        public double V;

        public IndexedValue(int index, double value)
        {
            Index = index;
            V = value;
        }

        public override string ToString()
        {
            return $"{Index}:{V}";
        }
    }

    public class ValueIndexComparer : IComparer<IndexedValue>
    {
        public int Compare(IndexedValue x, IndexedValue y) => x.Index.CompareTo(y.Index);
    }

    public class SparseVector : IEnumerable, IEnumerable<IndexedValue>
    {
        private double? normL1 = null;
        private double? normL2 = null;
        private double? mean = null;
        private List<IndexedValue> data = new List<IndexedValue>();

        public SparseVector(IEnumerable<double> values)
        {
            data = values.Select((v, i) => new IndexedValue(i, v)).ToList();
        }

        public SparseVector(IEnumerable<Tuple<int, double>> tuples)
        {
            data = FromTuples(tuples);
        }

        public SparseVector(SparseVector v)
        {
            data = FromTuples(v.EnumerateIndexed());
        }

        public int Count => data.Count;

        public double NormL1()
        {
            if (normL1 == null) normL1 = data.AsParallel().Sum(e => Math.Abs(e.V));
            return (double)normL1;
        }

        public double NormL2()
        {
            if (normL2 == null) normL2 = Math.Sqrt(data.AsParallel().Sum(e => e.V * e.V));
            return (double)normL2;
        }

        public double SquareNormL2() => data.AsParallel().Sum(e => e.V * e.V);

        public double Mean()
        {
            //Здесь учитываются только не нулевые элементы вектора
            if (mean == null) mean = Sum() / data.Count;
            return (double)mean;
        }

        public double Sum() => data.AsParallel().Sum(e => e.V);

        public double Dot(SparseVector v)
        {
            return this * v;
        }

        public double CosDistance(SparseVector v)
        {
            var divisor = NormL2() * v.NormL2();
            if (divisor == 0) return 0; //при нулевой длине одного из векеторов считаем, что ковариация=0
            return (this * v) / divisor;
        }

        public SparseVector BuildCentered()
        {
            double mx = Mean();
            return new SparseVector(data.AsParallel().Select(v => Tuple.Create(v.Index, v.V - mx)));
        }

        public void Center()
        {
            double mx = Mean();
            data.AsParallel().ForAll(x => x.V -= mx);
        }

        public void Normalize()
        {
            double d = NormL2();
            if (d == 0)
                data.ForEach(x => x.V = 0);
            else
                data.AsParallel().ForAll(x => x.V /= d);
        }

        public double Dispersion()
        {
            return BuildCentered().SquareNormL2();  //Затратно, т.к. строится центрированный вектор
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
                int index = data.BinarySearch(new IndexedValue(i, 0), new ValueIndexComparer());
                if (index >= 0) return data[i].V;
                throw new IndexOutOfRangeException();
            }
        }

        public static double operator *(SparseVector vector_a, SparseVector vector_b)
        {
            IEnumerator a_enumerator = vector_a.GetEnumerator();
            IEnumerator b_enumerator = vector_b.GetEnumerator();
            bool end = !(a_enumerator.MoveNext() && b_enumerator.MoveNext());
            double result = 0;
            while (!end)
            {
                IndexedValue a = (IndexedValue)a_enumerator.Current;
                IndexedValue b = (IndexedValue)b_enumerator.Current;
                if (a.Index == b.Index)
                {
                    result += a.V * b.V;
                    end = !(a_enumerator.MoveNext() && b_enumerator.MoveNext());
                }
                else if (a.Index < b.Index)
                    end = !a_enumerator.MoveNext();
                else if (a.Index > b.Index)
                    end = !b_enumerator.MoveNext();
                else
                    throw new IndexOutOfRangeException($"Неизвестное значение нумераторов {a_enumerator.ToString() + b_enumerator.ToString()}");
            }
            return result;
        }

        public IEnumerable<Tuple<int, double>> EnumerateIndexed()
        {
            return data.Select(t => Tuple.Create(t.Index, t.V));
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)data).GetEnumerator();
        }

        IEnumerator<IndexedValue> IEnumerable<IndexedValue>.GetEnumerator()
        {
            return ((IEnumerable<IndexedValue>)data).GetEnumerator();
        }

        public override string ToString()
        {
            return "[" + string.Join(",", data.AsParallel().Select(v => v.ToString())) + "]";
        }

        //-------------------------------------------------------------------------------------------------
        //Закрытые методы
        //-------------------------------------------------------------------------------------------------
        private static List<IndexedValue> FromTuples(IEnumerable<Tuple<int, double>> tuples)
        {
            return tuples
                .AsParallel()
                .OrderBy(t => t.Item1)
                .Select(t => new IndexedValue(t.Item1, t.Item2))
                .ToList();
        }

    }
}
