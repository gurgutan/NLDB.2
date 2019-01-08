using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NLDB.DAL
{
    public struct MatrixElement
    {
        public int Row;
        public int Column;
        public double Val;

        public ulong Index => (ulong)Row << 32 | (uint)Column;

        public MatrixElement(int row, int column, double val)
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
        private List<MatrixElement> data = new List<MatrixElement>();

        public SparseMatrix(IEnumerable<Tuple<int, int, double>> tuples)
        {
            data = tuples
                .AsParallel()
                .OrderBy(t => (ulong)t.Item1 << 32 | (uint)t.Item2)
                .Select(t => new MatrixElement(t.Item1, t.Item2, t.Item3))
                .ToList();
        }

        public SparseMatrix(SparseMatrix m)
        {
            data = m
                .EnumerateIndexed()
                .AsParallel()
                .OrderBy(t => (ulong)t.Item1 << 32 | (uint)t.Item2)
                .Select(t => new MatrixElement(t.Item1, t.Item2, t.Item3))
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

        public IEnumerable<Tuple<int, int, double>> EnumerateIndexed()
        {
            return data.Select(t => Tuple.Create(t.Row, t.Column, t.Val));
        }

        public void Transpose()
        {
            data.AsParallel().ForAll(e => e.Transpose());
            data.AsParallel().OrderBy(e => e.Index).ToList();
        }

        public static SparseMatrix Transpose(SparseMatrix a)
        {
            SparseMatrix result = new SparseMatrix(a);
            result.Transpose();
            return result;
        }

        //public static SparseMatrix Square(SparseMatrix a)
        //{
        //    SparseMatrix b = new SparseMatrix(a);
        //    List<Tuple<int, int, double>> tuples = new List<Tuple<int, int, double>>();
        //    IEnumerator a_enumerator = a.GetEnumerator();
        //    IEnumerator b_enumerator = b.GetEnumerator();
        //    bool end = !(a_enumerator.MoveNext() && b_enumerator.MoveNext());
        //    double result = 0;
        //    while (!end)
        //    {
        //        MatrixElement a_element = (MatrixElement)a_enumerator.Current;
        //        MatrixElement b_element = (MatrixElement)b_enumerator.Current;
        //        int row = a_element.Row;
        //        var column = b_element.Row;
        //        if (a_element.Row == b_element.Row && a_element.Column == b_element.Column)
        //            result += a_element.Val * b_element.Val;
        //        if (a_element.Index < b_element.Index)
        //            end = !a_enumerator.MoveNext();
        //        else if (a_element.Index > b_element.Index)
        //            end = !b_enumerator.MoveNext();
        //        else
        //            end = !(a_enumerator.MoveNext() && b_enumerator.MoveNext());
        //        int next_row = a_element.Row;
        //        int next_column = b_element.Row;
        //        if (next_row != row || next_column != column)
        //        {

        //        }
        //    }
        //    return result;
        //}

        //public static double operator *(SparseMatrix a, SparseMatrix b)
        //{
        //    IEnumerator a_enumerator = a.GetEnumerator();
        //    IEnumerator b_enumerator = b.GetEnumerator();
        //    bool end = !(a_enumerator.MoveNext() && b_enumerator.MoveNext());
        //    double result = 0;
        //    while (!end)
        //    {
        //        TripletValue a_element = (TripletValue)a_enumerator.Current;
        //        TripletValue b_element = (TripletValue)b_enumerator.Current;
        //        if (a_element.Row == b_element.Row && a_element.Column == b_element.Column)
        //            result += a_element.Val * b_element.Val;
        //        if (a_element.Index < b_element.Index)
        //            end = !a_enumerator.MoveNext();
        //        else if (a_element.Index > b_element.Index)
        //            end = !b_enumerator.MoveNext();
        //        else
        //            end = !(a_enumerator.MoveNext() && b_enumerator.MoveNext());
        //    }
        //    return result;
        //}

        public override string ToString()
        {
            return data.Aggregate("", (c, n) => c + (c == "" ? "" : ",") + n.ToString());
        }


        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)data).GetEnumerator();
        }
    }
}
