using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLDB.Utils;

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

        public SparseMatrix(IEnumerable<(int, int, double)> tuples)
        {
            rows = tuples
                .AsParallel()
                .OrderBy(t => (ulong)t.Item1 << 32 | (uint)t.Item2)
                .GroupBy(t => t.Item1)
                .Select(group => new IndexedVector(group.Key, new SparseVector(group.Select(v => Tuple.Create(v.Item2, v.Item3)))))
                .ToList();
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

        public void NormalizeRows()
        {
            rows.AsParallel().ForAll(r => r.V.Normalize());
        }

        public void CenterRows()
        {
            rows.AsParallel().ForAll(r => r.V.Center());
        }

        public SparseMatrix BuildRowsCovariation()
        {
            ConcurrentDictionary<int, SparseVector> indexedVectors = new ConcurrentDictionary<int, SparseVector>();
            using (ProgressInformer informer = new ProgressInformer(prompt: $"Матрица подобия:", max: rows.Count, measurment: $"слов", barSize: 64))
            {
                long rowcount = 0;
                Parallel.ForEach(rows, (r) =>
                //foreach (var r in rows)
                {
                    List<Tuple<int, double>> tuples = new List<Tuple<int, double>>();
                    foreach (IndexedVector c in rows)
                    {
                        SparseVector x = r.V;
                        SparseVector y = c.V;
                        if (x.Count > 0 && y.Count > 0)
                        {
                            double cxy = x * y;
                            tuples.Add(Tuple.Create(c.Index, cxy));
                        }
                    }
                    if (tuples.Count > 0) indexedVectors[r.Index] = new SparseVector(tuples);
                    rowcount++;
                    if (rowcount % 37 == 0) informer.Set(rowcount);
                });
                informer.Set(rows.Count);
            }
            return new SparseMatrix(indexedVectors);
        }

        //-------------------------------------------------------------------------------------------------
        // Методы преобразования
        //-------------------------------------------------------------------------------------------------
        public void Transpose()
        {
            rows = FromIndexed(EnumerateIndexed().Select(t => Tuple.Create(t.Item2, t.Item1, t.Item3)));
        }

        /// <summary>
        /// Удаляет элементы матрицы, значения которых находятся в диапазоне [left, right] включительно.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public int RemoveValuesFromRange(double left, double right)
        {
            int count = 0;
            //Для каждого вектора выполняем операцию удаления элементов из интервала
            rows.ForEach(v => count += v.V.RemoveValuesFromRange(left, right));
            //Теперь удаляем "пустые" векторы
            rows.RemoveAll(v => v.V.Count == 0);
            return count;
        }

        //-------------------------------------------------------------------------------------------------
        // Статические методы
        //-------------------------------------------------------------------------------------------------
        public static SparseMatrix BuildTransposed(SparseMatrix a)
        {
            SparseMatrix result = new SparseMatrix(a);
            result.Transpose();
            return result;
        }
        /// <summary>
        /// Вычисление ковариации двух матриц. Каждая строка матрицы a скалярно умножается на строку матрицы b. Векторы-строки должны быть предварительно центрированы.
        /// </summary>
        /// <param name="a">левая матрица</param>
        /// <param name="b">правая матрица (предполагается что она уже транспонирована)</param>
        /// <param name="zeroingRadius">элементы, не из интервала [-zeroingRadius, zeroingRadius] в результат не записываются</param>
        /// <returns></returns>
        public static SparseMatrix CovariationSlow(SparseMatrix a, SparseMatrix b, double zeroingRadius = 0)
        {
            if (zeroingRadius < 0) zeroingRadius = -zeroingRadius;
            ConcurrentDictionary<int, SparseVector> indexedVectors = new ConcurrentDictionary<int, SparseVector>();
            Parallel.ForEach(a.Rows, (r) =>
            {
                List<Tuple<int, double>> tuples = new List<Tuple<int, double>>();
                foreach (IndexedVector c in b.Rows)
                {
                    if (c == r)
                        tuples.Add(Tuple.Create(c.Index, 1.0));
                    else if (r.V.Count > 0 && c.V.Count > 0)
                    {
                        //Скалярное произведение
                        double cxy = r.V * c.V;
                        //Нулевые элементы в результат не добавляем
                        if (cxy < -zeroingRadius || cxy > zeroingRadius)
                            tuples.Add(Tuple.Create(c.Index, cxy));
                    }
                }
                if (tuples.Count > 0) indexedVectors[r.Index] = new SparseVector(tuples);
            });
            return new SparseMatrix(indexedVectors);
        }

        public static SparseMatrix Covariation(SparseMatrix a, SparseMatrix b, double zeroingRadius = 0)
        {
            if (zeroingRadius < 0) zeroingRadius = -zeroingRadius;
            var tuples = a.SelectMany(r => b.Select(c => (r, c)))
                .AsParallel()
                .Select(t =>
                {
                    if (t.r.V.Count > 0 && t.c.V.Count > 0)
                        return (t.r.Index, t.c.Index, t.r.V * t.c.V);
                    else
                        return (t.r.Index, t.c.Index, 0);
                })
                .Where(t => t.Item3 > zeroingRadius || t.Item3 < -zeroingRadius);
            return new SparseMatrix(tuples);
        }

        public static double operator *(SparseMatrix a, SparseMatrix b)
        {
            throw new NotImplementedException();

        }

        IEnumerator<IndexedVector> IEnumerable<IndexedVector>.GetEnumerator()
        {
            return ((IEnumerable<IndexedVector>)rows).GetEnumerator();
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

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable<IndexedVector>)rows).GetEnumerator();
        }
    }
}
