using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public struct Pointer:IComparable<Pointer>
    {
        public int id;
        public int count;
        public float confidence;

        public Pointer(int _id, int _number, float _conf)
        {
            id = _id;
            count = _number;
            confidence = _conf;
        }

        public override int GetHashCode()
        {
            return id;
        }

        public override bool Equals(object obj)
        {
            return ((Pointer)obj).id == this.id;
        }

        public int GetHashCode(object obj)
        {
            return id;
        }

        public float Average() => confidence / count;

        int IComparable<Pointer>.CompareTo(Pointer other)
        {
            float this_average = this.Average();
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
            return ((Pointer)x).id == ((Pointer)y).id;
        }

        public new bool Equals(object x, object y)
        {
            return ((Pointer)x).id == ((Pointer)y).id;
        }

        public int GetHashCode(Pointer obj)
        {
            return ((Pointer)obj).id;
        }

    }





}
