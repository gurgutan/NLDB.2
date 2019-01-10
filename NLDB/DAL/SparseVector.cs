using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NLDB.DAL
{
    public class VectorValue
    {
        public readonly int Index;
        public double V;

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

    public class SparseVector : IEnumerable, IEnumerable<VectorValue>
    {
        private double? normL1 = null;
        private double? normL2 = null;
        private double? mean = null;
        private List<VectorValue> data = new List<VectorValue>();

        public SparseVector(IEnumerable<double> values)
        {
            data = values.Select((v, i) => new VectorValue(i, v)).ToList();
        }

        public SparseVector(IEnumerable<Tuple<int, double>> tuples)
        {
            data = FromTuples(tuples);
        }

        public SparseVector(SparseVector v)
        {
            data = SparseVector.FromTuples(v.EnumerateIndexed());
            //data.Sort(new Comparison<VectorValue>((v1, v2) => v2.Index - v1.Index));
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

        public double Dot(SparseVector v)
        {
            return this * v;
        }

        public double CosDistance(SparseVector v)
        {
            return (this * v) / (NormL2 * v.NormL2);
        }

        public SparseVector BuildCentered
        {
            get
            {
                double mx = Mean;
                return new SparseVector(data.Select(v => Tuple.Create(v.Index, v.V - mx)));
            }
        }

        public void Center()
        {
            double mx = Mean;
            data.AsParallel().ForAll(x => x.V -= mx);
        }

        public void Normalize()
        {
            double d = NormL2;
            data.ForEach(x => x.V = x.V / d);
        }

        public double Dispersion
        {
            get
            {
                SparseVector center = BuildCentered;
                return center.SquareNormL2;
            }
        }

        public double this[int i]
        {
            get
            {
                int index = data.Select(v => v.Index).ToList().BinarySearch(i);
                if (index >= 0) return data[i].V;
                else throw new IndexOutOfRangeException();
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

        IEnumerator<VectorValue> IEnumerable<VectorValue>.GetEnumerator()
        {
            return ((IEnumerable<VectorValue>)data).GetEnumerator();
        }

        public override string ToString()
        {
            return "[" + string.Join(",", data.Select(v => v.ToString())) + "]";
        }

        //-------------------------------------------------------------------------------------------------
        //Закрытые методы
        //-------------------------------------------------------------------------------------------------
        private static List<VectorValue> FromTuples(IEnumerable<Tuple<int, double>> tuples)
        {
            var result = tuples
                .AsParallel()
                .OrderBy(t => t.Item1)
                .Select(t => new VectorValue(t.Item1, t.Item2))
                .ToList();
            //result.Sort(new Comparison<VectorValue>((v1, v2) => v2.Index - v1.Index));
            return result;
        }

    }
}
