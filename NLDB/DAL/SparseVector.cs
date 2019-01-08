using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NLDB.DAL
{
    public struct TripletValue
    {
        public int Row;
        public int Column;
        public double Val;

        public ulong Index
        {
            get => (ulong)Row << 32 | (uint)Column;
        }

        public TripletValue(int row, int column, double val)
        {
            Row = row;
            Column = column;
            Val = val;
        }

        public void Transpose()
        {
            int tmp = Row;
            Row = Column;
            Column = tmp;
        }

        public override string ToString()
        {
            return $"({Row},{Column},{Val})";
        }
    }

    public class SparseMatrix : IEnumerable
    {
        private double? normL1 = null;
        private double? normL2 = null;
        private List<TripletValue> data = new List<TripletValue>();

        public SparseMatrix(IEnumerable<Tuple<int, int, double>> tuples)
        {
            data = tuples
                .AsParallel()
                .OrderBy(t => (ulong)t.Item1 << 32 | (uint)t.Item2)
                .Select(t => new TripletValue(t.Item1, t.Item2, t.Item3))
                .ToList();
        }

        public int Count
        {
            get => data.Count;
        }

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

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)data).GetEnumerator();
        }

        public void Transpose()
        {
            data.AsParallel().ForAll(e => e.Transpose());
            data.AsParallel().OrderBy(e => e.Index).ToList();
        }

        public static double operator *(SparseMatrix a, SparseMatrix b)
        {
            var a_enumerator = a.GetEnumerator();
            var b_enumerator = b.GetEnumerator();
            bool end = !(a_enumerator.MoveNext() && b_enumerator.MoveNext());
            double result = 0;
            while (!end)
            {
                var a_element = (TripletValue)a_enumerator.Current;
                var b_element = (TripletValue)b_enumerator.Current;
                if (a_element.Row == b_element.Row && a_element.Column == b_element.Column)
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

        public override string ToString()
        {
            return data.Aggregate("", (c, n) => c + (c == "" ? "" : ",") + n.ToString());
        }


    }
}
