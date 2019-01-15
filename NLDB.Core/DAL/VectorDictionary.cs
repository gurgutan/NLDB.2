using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;

namespace NLDB.DAL
{
    public class VectorDictionary : Dictionary<int, double>
    {
        private double? size = null;
        public double Size
        {
            get
            {
                if (size == null) size = Values.Sum(v => v * v);
                return Math.Sqrt((double)size);
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

        public VectorDictionary()
        {
        }

        public VectorDictionary(int capacity) : base(capacity)
        {
        }
    }

    public class MatrixDictionary : Dictionary<int, VectorDictionary>
    {
        public MatrixDictionary()
        {
        }

        public MatrixDictionary(int capacity) : base(capacity)
        {
        }
    }
}
