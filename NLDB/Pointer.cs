using System;
using System.Collections.Generic;

namespace NLDB
{
    public struct Pointer : IComparable<Pointer>
    {
        public int id;
        public int count;
        public float value;

        public Pointer(int _id, int _number, float _conf)
        {
            id = _id;
            count = _number;
            value = _conf;
        }

        public override int GetHashCode()
        {
            return id;
        }

        public override bool Equals(object obj)
        {
            return ((Pointer)obj).id == id;
        }

        public int GetHashCode(object obj)
        {
            return id;
        }

        public float Average() => value / count;

        int IComparable<Pointer>.CompareTo(Pointer other)
        {
            float this_average = Average();
            float other_average = other.Average();
            if (other_average > this_average)
                return -1;
            else if (other_average == this_average)
                return 0;
            else
                return 1;
        }

    }

    public class PointerComparer : IEqualityComparer<Pointer>
    {
        public bool Equals(Pointer x, Pointer y)
        {
            return x.id == y.id;
        }

        public new bool Equals(object x, object y)
        {
            return ((Pointer)x).id == ((Pointer)y).id;
        }

        public int GetHashCode(Pointer obj)
        {
            return obj.id;
        }

    }





}
