using System.Collections.Generic;
using System.Linq;

namespace NLDB.DAL
{
    public class SparseVector : Dictionary<int, double>
    {
        private double? size = null;
        public double Size
        {
            get
            {
                if (size == null) size = Values.Sum(v => v * v);
                return (double)size;
            }
        }

        public new double this[int i]
        {
            get => base[i];
            set
            {
                size = null;
                base[i] = value;
            }
        }

        public new void Add(int i, double v)
        {
            size = null;
            base.Add(i, v);
        }

        public new void Remove(int i)
        {
            size = null;
            base.Remove(i);
        }

        public new void Clear()
        {
            size = null;
            base.Clear();
        }

        public SparseVector()
        {
        }

        public SparseVector(int capacity) : base(capacity)
        {
        }
    }

    public class SparseMatrix : Dictionary<int, SparseVector>
    {
        public SparseMatrix()
        {
        }

        public SparseMatrix(int capacity) : base(capacity)
        {
        }
    }
}
