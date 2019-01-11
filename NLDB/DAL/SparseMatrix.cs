using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NLDB.DAL
{
    public class IndexedVector
    {
        public readonly int Index;
        public readonly SparseVector V;

        public IndexedVector(int index, SparseVector v)
        {
            Index = index;
            V = v;
        }

        public override string ToString()
        {
            return $"({Index},{V.ToString()})";
        }
    }

    public class SparseMatrix : IEnumerable<IndexedVector>
    {
        private double? mean = null;
        private double? sum = null;
        private double? normL1 = null;
        private double? normL2 = null;
        private List<IndexedVector> rows = new List<IndexedVector>();
        public IList<IndexedVector> Rows => rows;

        public SparseMatrix(IEnumerable<Tuple<int, int, double>> tuples)
        {
            rows = FromIndexed(tuples);
        }

        public SparseMatrix(IEnumerable<Tuple<int, SparseVector>> tuples)
        {
            rows = tuples.OrderBy(t => t.Item1).Select(t => new IndexedVector(t.Item1, t.Item2)).ToList();
        }
        public SparseMatrix(IDictionary<int, SparseVector> pairs)
        {
            rows = pairs.OrderBy(t => t.Key).Select(t => new IndexedVector(t.Key, t.Value)).ToList();
        }


        public SparseMatrix(SparseMatrix m)
        {
            rows = FromIndexed(m.EnumerateIndexed());
        }

        private List<IndexedVector> FromIndexed(IEnumerable<Tuple<int, int, double>> tuples)
        {
            return tuples
                .AsParallel()
                .OrderBy(t => (ulong)t.Item1 << 32 | (uint)t.Item2)
                .GroupBy(t => t.Item1)
                .Select(group => new IndexedVector(group.Key, new SparseVector(group.Select(v => Tuple.Create(v.Item2, v.Item3)))))
                .ToList();
        }

        public int Count => rows.Count;

        public IEnumerable<Tuple<int, int, double>> EnumerateIndexed()
        {
            return rows.SelectMany(row => row.V
                    .EnumerateIndexed()
                    .Select(v => Tuple.Create(row.Index, v.Item1, v.Item2)));
        }

        public void Transpose()
        {
            rows = FromIndexed(EnumerateIndexed().Select(t => Tuple.Create(t.Item2, t.Item1, t.Item3)));
        }

        public static SparseMatrix BuildTransposed(SparseMatrix a)
        {
            SparseMatrix result = new SparseMatrix(a);
            result.Transpose();
            return result;
        }

        public double NormL1()
        {
            if (normL1 == null) normL1 = rows.AsParallel().Sum(e => Math.Abs(e.V.NormL1()));
            return (double)normL1;
        }

        public double NormL2()
        {
            if (normL2 == null) normL2 = Math.Sqrt(rows.AsParallel().Sum(e => e.V.NormL2()));
            return (double)normL2;
        }

        public double Sum()
        {
            if (sum == null) sum = rows.AsParallel().Sum(r => r.V.Sum());
            return (double)sum;
        }

        public double Mean()
        {
            //Учитываются только ненулевые строки
            if (mean == null) mean = Sum() / rows.Count;
            return (double)mean;
        }

        public void NormalizeRows() => rows.AsParallel().ForAll(r => r.V.Normalize());


        public void CenterRows() => rows.AsParallel().ForAll(r => r.V.Center());

        public SparseMatrix BuildRowsCovariation()
        {
            ConcurrentDictionary<int, SparseVector> indexedVectors = new ConcurrentDictionary<int, SparseVector>();
            using (ProgressInformer informer = new ProgressInformer(prompt: $"Матрица подобия:", max: rows.Count, measurment: $"слов", barSize: 64))
            {
                //Parallel.ForEach(rows, (r) =>
                foreach (var r in rows)
                {
                    List<Tuple<int, double>> tuples = new List<Tuple<int, double>>();
                    foreach (IndexedVector c in rows)
                    {
                        var x = r.V;
                        var y = c.V;
                        if (x.Count > 0 && y.Count > 0)
                        {
                            var cxy = x * y;
                            tuples.Add(Tuple.Create(c.Index, cxy));
                        }
                    }
                    if (tuples.Count > 0) indexedVectors[r.Index] = new SparseVector(tuples);
                }//);
            }
            return new SparseMatrix(indexedVectors);
        }

        public static double operator *(SparseMatrix a, SparseMatrix b)
        {
            throw new NotImplementedException();

        }

        IEnumerator<IndexedVector> IEnumerable<IndexedVector>.GetEnumerator()
        {
            return ((IEnumerable<IndexedVector>)rows).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return rows.GetEnumerator();
        }

        public override string ToString()
        {
            return rows.Aggregate("", (c, n) => c + (c == "" ? "" : "\n") + n.ToString());
        }

        public string ToString(string format)
        {
            if (format.ToLower() == "f")
            {
                throw new NotImplementedException();
            }
            else
                return rows.Aggregate("", (c, n) => c + (c == "" ? "" : "\n") + n.ToString());
        }
    }
}
