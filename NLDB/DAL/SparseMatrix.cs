using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NLDB.DAL
{
    public struct MatrixRow
    {
        public readonly int Index;
        public readonly SparseVector V;

        public MatrixRow(int index, SparseVector v)
        {
            Index = index;
            V = v;
        }

        public override string ToString()
        {
            return $"({Index},{V.ToString()})";
        }
    }

    public class SparseMatrix : IEnumerable<MatrixRow>
    {
        private double? normL1 = null;
        private double? normL2 = null;
        private List<MatrixRow> rows = new List<MatrixRow>();
        public IEnumerable<MatrixRow> Rows => rows.AsEnumerable();

        public SparseMatrix(IEnumerable<Tuple<int, int, double>> tuples)
        {
            rows = FromTuples(tuples);
        }

        public SparseMatrix(SparseMatrix m)
        {
            rows = FromTuples(m.EnumerateIndexed());
        }

        private List<MatrixRow> FromTuples(IEnumerable<Tuple<int, int, double>> tuples)
        {
            return tuples
                .AsParallel()
                .OrderBy(t => (ulong)t.Item1 << 32 | (uint)t.Item2)
                .GroupBy(t => t.Item1)
                .Select(group =>
                    new MatrixRow(group.Key, new SparseVector(group.Select(v => Tuple.Create(v.Item2, v.Item3)))))
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
            rows = FromTuples(EnumerateIndexed().Select(t => Tuple.Create(t.Item2, t.Item1, t.Item3)));
        }

        public static SparseMatrix Transpose(SparseMatrix a)
        {
            SparseMatrix result = new SparseMatrix(a);
            result.Transpose();
            return result;
        }

        public double NormL1
        {
            get
            {
                if (normL1 == null) normL1 = rows.AsParallel().Sum(e => Math.Abs(e.V.NormL1));
                return (double)normL1;
            }
        }

        public double NormL2
        {
            get
            {
                if (normL2 == null) normL2 = Math.Sqrt(rows.AsParallel().Sum(e => e.V.NormL2));
                return (double)normL2;
            }
        }

        public double Mean => throw new NotImplementedException();

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

        public void NormalizeRows() => rows./*AsParallel().ForAll*/ForEach(r => r.V.Normalize());

        public void CenterRows() => rows./*AsParallel().ForAll*/ForEach(r => r.V.Center());

        public SparseMatrix RowsCorrelationMatrix()
        {
            List<Tuple<int, int, double>> result = new List<Tuple<int, int, double>>();
            SparseMatrix transposed = new SparseMatrix(this);
            using (ProgressInformer informer = new ProgressInformer(prompt: $"Матрица подобия:", max: rows.Count, measurment: $"слов", barSize: 64))
            {
                int count = 0;
                foreach (MatrixRow r in rows)
                {
                    foreach (MatrixRow c in transposed.rows)
                    {
                        SparseVector xc = r.V.BuildCentered;
                        SparseVector yc = c.V.BuildCentered;
                        if (xc.Count > 0 && yc.Count > 0)
                        {
                            double kxy = (xc * yc) / (xc.NormL2 * yc.NormL2);
                            result.Add(Tuple.Create(r.Index, c.Index, kxy));
                        }
                    }
                    informer.Set(count++);
                }
            }
            return new SparseMatrix(result);
        }

        public SparseMatrix RowsCovariationMatrix()
        {
            List<Tuple<int, int, double>> result = new List<Tuple<int, int, double>>();
            using (ProgressInformer informer = new ProgressInformer(prompt: $"Матрица подобия:", max: rows.Count, measurment: $"слов", barSize: 64))
            {
                int count = 0;
                foreach (MatrixRow r in rows)
                {
                    foreach (MatrixRow c in rows)
                    {
                        var x = r.V;
                        var y = c.V;
                        if (x.Count > 0 && y.Count > 0)
                        {
                            double kxy = x * y;
                            result.Add(Tuple.Create(r.Index, c.Index, kxy));
                        }
                    }
                    if (count++ % 101 == 0) informer.Set(count);
                }
            }
            return new SparseMatrix(result);
        }

        public static double operator *(SparseMatrix a, SparseMatrix b)
        {
            throw new NotImplementedException();

        }

        IEnumerator<MatrixRow> IEnumerable<MatrixRow>.GetEnumerator()
        {
            return ((IEnumerable<MatrixRow>)rows).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return rows.GetEnumerator();
        }
    }
}
