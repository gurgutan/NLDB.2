using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public struct Link
    {
        public int id;
        public int number;
        public float confidence;

        public Link(int _id, int _number, float _conf)
        {
            id = _id;
            number = _number;
            confidence = _conf;
        }

        public override int GetHashCode()
        {
            return id;
        }

        public override bool Equals(object obj)
        {
            return ((Link)obj).id == this.id;
        }

        public int GetHashCode(object obj)
        {
            return id;
        }
    }

    public class LinkComparer : IEqualityComparer<Link>
    {
        public bool Equals(Link x, Link y)
        {
            return ((Link)x).id == ((Link)y).id;
        }

        public new bool Equals(object x, object y)
        {
            return ((Link)x).id == ((Link)y).id;
        }

        public int GetHashCode(Link obj)
        {
            return ((Link)obj).id;
        }

    }





}
