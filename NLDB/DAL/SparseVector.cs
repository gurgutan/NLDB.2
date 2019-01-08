using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NLDB.DAL
{
    public struct VectorValue
    {
        public int Index;
        public double Val;

        public VectorValue(int index, double val)
        {
            Index = index;
            Val = val;
        }

        public override string ToString()
        {
            return $"({Index},{Val})";
        }
    }

    public class SparseVector : IEnumerable
    {
        private double? normL1 = null;
        private double? normL2 = null;
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
                if (normL1 == null) normL1 = data.AsParallel().Sum(e => Math.Abs(e.Val));
                return (double)normL1;
            }
        }

        public double NormL2
        {
            get
            {
                if (normL2 == null) normL2 = Math.Sqrt(data.AsParallel().Sum(e => e.Val * e.Val));
                return (double)normL2;
            }
        }

        public static double operator *(SparseVector a, SparseVector b)
        {
            IEnumerator a_enumerator = a.GetEnumerator();
            IEnumerator b_enumerator = b.GetEnumerator();
            bool end = !(a_enumerator.MoveNext() && b_enumerator.MoveNext());
            double result = 0;
            while (!end)
            {
                VectorValue a_element = (VectorValue)a_enumerator.Current;
                VectorValue b_element = (VectorValue)b_enumerator.Current;
                if (a_element.Index == b_element.Index)
                    result += a_element.Val * b_element.Val;
                if (a_element.Index < b_element.Index)
                    end = !a_enumerator.MoveNext();
                else if (a_element.Index > b_element.Index)
                    end = !b_enumerator.MoveNext();
                else
                    end = !(a_enumerator.MoveNext() && b_enumerator.MoveNext());
            }
            return result;
        }

        public IEnumerable<Tuple<int, double>> EnumerateIndexed()
        {
            return data.Select(t => Tuple.Create(t.Index, t.Val));
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)data).GetEnumerator();
        }
    }
