using System.Collections.Generic;
using System.Data.SQLite;
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

        public float Average()
        {
            return sum / count;
        }
    }

    /// <summary>
    /// Класс,представляющий матрицу позиционных расстояний между словами
    /// </summary>
    public class DMatrix
    {
        //Данные матрицы представлены словарем, содержащим строки матрицы. Строка матрицы также представлена словарем.
        //ключ строки и ключ столбца - id Слов.
        private Dictionary<int, Dictionary<int, DInfo>> m;

        public DMatrix()
        {
            m = new Dictionary<int, Dictionary<int, DInfo>>();
        }

        private bool ContainsRow(int row)
        {
            return m.ContainsKey(row);
        }

        /// <summary>
        /// Возвращает true, если в строке r и колонке c существует запись. Иначе возвращает false
        /// </summary>
        /// <param name="r">номер строки</param>
        /// <param name="c">номер столбца</param>
        /// <returns></returns>
        public bool Contains(int r, int c)
        {
            var row = GetRow(r);
            if (row == null) return false;
            if (!row.ContainsKey(c)) return false;
            return true;
        }

        /// <summary>
        /// Возвращает строку матрицы, если она существует и null в противном случае
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        private Dictionary<int, DInfo> GetRow(int r)
        {
            if (m.TryGetValue(r, out Dictionary<int, DInfo> row))
                return row;
            return null;
        }

        public DInfo this[int r, int c]
        {
            get
            {
                var row = GetRow(r);
                if (row == null) return new DInfo();
                if (!row.TryGetValue(c, out DInfo info))
                    return new DInfo();
                return info;
            }
            set
            {
                var row = GetRow(r);
                if (row == null)
                    m[r] = new Dictionary<int, DInfo>();
                m[r][c] = value;
            }
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
            var row = GetRow(r);
            if (row == null) return;
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

        /// <summary>
        /// Возвращает Pointer(id, count, dist),  наименьший по критерию dist/count из всех элементов матрицы строки r
        /// </summary>
        /// <param name="r">id строки матрицы</param>
        /// <returns></returns>
        public Pointer Min(int r)
        {
            var row = GetRow(r);
            //Если нет строки в матрице или нет значения - значит у слова нет соседних. В этом случае возвращаем
            //специальный элемент - id=0, count=0, dist="бесконечность"
            if (row == null || row.Count == 0)
                return new Pointer(0, 0, float.MaxValue);
            var first = new Pointer(row.First().Key, row.First().Value.count, row.First().Value.sum);
            //TODO: переделать на параллельный Aggregate
            Pointer min = row.Aggregate(first,
                (c, n) =>
                {
                    float dist = n.Value.sum / n.Value.count;
                    if (dist < c.value)
                        return new Pointer(n.Key, n.Value.count, dist);
                    return c;
                });
            return min;
        }

        /// <summary>
        /// Возвращает Pointer(id, count, dist), наибольший по критерию dist/count из всех элементов матрицы строки r
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public Pointer Max(int r)
        {
            var row = GetRow(r);
            if (row == null || row.Count == 0)
                return new Pointer(0, 0, float.MaxValue);
            var first = new Pointer(row.First().Key, row.First().Value.count, row.First().Value.sum);
            //TODO: переделать на параллельный Aggregate
            Pointer max = row.Aggregate(first,
                (c, n) =>
                {
                    var dist = n.Value.sum / n.Value.count;
                    if (dist > c.value)
                        return new Pointer(n.Key, n.Value.count, dist);
                    return c;
                });
            return max;
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
                return new Pointer(e.id, e.count * info.count, e.value * info.sum);
            }).ToList();
        }

        public IList<Pointer> ElementswiseSum(IEnumerable<Pointer> v, int r)
        {
            if (!m.TryGetValue(r, out Dictionary<int, DInfo> row))
                return new List<Pointer>();
            return v.Select(e =>
            {
                DInfo info = row[e.id];
                return new Pointer(e.id, e.count + info.count, e.value + info.sum);
            }).ToList();
        }
    }
}
