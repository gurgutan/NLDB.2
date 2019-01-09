using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NLDB.DAL
{
    public struct VectorValue
    {
        public readonly int Index;
        public readonly double V;

        public VectorValue(int index, double value)
        {
            Index = index;
            V = value;
        }

        public override string ToString()
        {
            return $"({Index},{V})";
        }
    }

    public class SparseVector : IEnumerable
    {
        private double? normL1 = null;
        private double? normL2 = null;
        private double? mean = null;
        private List<VectorValue> data = new List<VectorValue>();

        public SparseVector(IEnumerable<Tuple<int, double>> tuples)
        {
            data = tuples
                .AsParallel()
                .OrderBy(t => (ulong)t.Item1)
                .Select(t => new VectorValue(t.Item1, t.Item2))
                .ToList();
        }

        public SparseVector(SparseVector v)
        {
            data = v
                .EnumerateIndexed()
                .AsParallel()
                .OrderBy(t => (ulong)t.Item1)
                .Select(t => new VectorValue(t.Item1, t.Item2))
                .ToList();
        }

        public int Count => data.Count;

        public double NormL1
        {
            get
            {
                if (normL1 == null) normL1 = data.AsParallel().Sum(e => Math.Abs(e.V));
                return (double)normL1;
            }
        }

        public double NormL2
        {
            get
            {
                if (normL2 == null) normL2 = Math.Sqrt(data.AsParallel().Sum(e => e.V * e.V));
                return (double)normL2;
            }
        }

        public double SquareNormL2 => data.AsParallel().Sum(e => e.V * e.V);

        public double Mean
        {
            get
            {
                if (mean == null) mean = Sum / data.Count;
                return (double)mean;
            }
        }

        public double Sum => data.AsParallel().Sum(e => e.V);

        public double Multiply(SparseVector v)
        {
            return this * v;
        }

        public double CosDistance(SparseVector v)
        {
            return this * v / (NormL2 * v.NormL2);
        }

        public SparseVector Centered
        {
            get
            {
                double mx = Mean;
                return new SparseVector(data.Select(v => Tuple.Create(v.Index, v.V - mx)));
            }
        }

        public double Dispersion
        {
            get
            {
                SparseVector center = Centered;
                return center.SquareNormL2;
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
                VectorValue a = (VectorValue)a_enumerator.Current;
                VectorValue b = (VectorValue)b_enumerator.Current;
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

        public override string ToString()
        {
            return "[" + string.Join(",", data.Select(v => v.ToString())) + "]";
        }
    }
}
