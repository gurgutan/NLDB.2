using System.Collections.Generic;
using System.Linq;

namespace NLDB
{
    public struct DInfo
    {
        public int count;
        public float sum;
        public DInfo(int c, float s)
        {
            count = c;
            sum = s;
        }
    }

    public class DMatrix
    {
        private Dictionary<int, Dictionary<int, DInfo>> m;

        private bool ContainsColumn(int col)
        {
            return m.ContainsKey(col);
        }

        public DInfo this[int r, int c]
        {
            get
            {
                if (!m.TryGetValue(r, out Dictionary<int, DInfo> row))
                    return new DInfo();
                if (!row.TryGetValue(c, out DInfo info))
                    return new DInfo();
                return info;
            }
            set
            {
                if (!m.TryGetValue(r, out Dictionary<int, DInfo> row))
                    m[r] = new Dictionary<int, DInfo>();
                m[r][c] = value;
            }
        }

        /// <summary>
        /// Возвращает true, если в строке r и колонке c существует запись. Иначе возвращает false
        /// </summary>
        /// <param name="r">номер строки</param>
        /// <param name="c">номер столбца</param>
        /// <returns></returns>
        public bool Contains(int r, int c)
        {
            if (!m.TryGetValue(r, out Dictionary<int, DInfo> row)) return false;
            if (!row.ContainsKey(c)) return false;
            return true;
        }

        /// <summary>
        /// Добавляет информацию о расстоянии между словами r и c в матрицу расстояний
        /// </summary>
        /// <param name="r"></param>
        /// <param name="c"></param>
        /// <param name="dist"></param>
        public void Add(int r, int c, float dist)
        {
            var value = this[r, c];
            value.count++;
            value.sum += dist;
            this[r, c] = value;
        }

        /// <summary>
        /// Удаляет данные из ячейки [r, c] матрицы расстояний
        /// </summary>
        /// <param name="r"></param>
        /// <param name="c"></param>
        public void Remove(int r, int c)
        {
            if (!m.TryGetValue(r, out Dictionary<int, DInfo> row)) return;
            if (row.ContainsKey(c)) row.Remove(c);
        }

        /// <summary>
        /// Возвращает среднее расстояние между словами r и c на основе данных, хранимых в ячейке [r, c]
        /// </summary>
        /// <param name="r"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public double GetAvarage(int r, int c)
        {
            return this[r, c].sum / this[r, c].count;
        }

        public Pointer Min(int r)
        {
            if (!m.TryGetValue(r, out Dictionary<int, DInfo> row))
                return new Pointer();
            var first = new Pointer(row.First().Key,row.First().Value.count, row.First().Value.sum)
            Pointer min = row.Aggregate(new Pointer(row.First().Key,row.));
        }

        /// <summary>
        /// Поэлементное умножение вектора v на колонку r
        /// </summary>
        /// <param name="v"></param>
        /// <param name="r"></param>
        public IList<Pointer> ElementwiseProduct(IEnumerable<Pointer> v, int r)
        {
            if (!m.TryGetValue(r, out Dictionary<int, DInfo> row))
                return new List<Pointer>();
            return v.Select(e =>
            {
                DInfo info = row[e.id];
                return new Pointer(e.id, e.count * info.count, e.confidence * info.sum);
            }).ToList();
        }

        public IList<Pointer> ElementswiseSum(IEnumerable<Pointer> v, int r)
        {
            if (!m.TryGetValue(r, out Dictionary<int, DInfo> row))
                return new List<Pointer>();
            return v.Select(e =>
            {
                DInfo info = row[e.id];
                return new Pointer(e.id, e.count + info.count, e.confidence + info.sum);
            }).ToList();
        }
    }
}
