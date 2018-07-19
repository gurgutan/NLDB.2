using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    [Serializable]
    public struct TripleInt
    {
        public int val;
        public int row;
        public int col;

        public TripleInt(int v, int r, int c)
        {
            this.val = v;
            this.row = r;
            this.col = c;
        }

        /// <summary>
        /// Сравнение для сортировки сначала по строкам, затем по колонкам
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int Compare(TripleInt a, TripleInt b)
        {
            if (a.row > b.row) return 1;
            if (a.row < b.row) return -1;
            if (a.col > b.col) return 1;
            if (a.col < b.col) return -1;
            return 0;
        }
    }
}
