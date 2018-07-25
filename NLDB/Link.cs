using System;
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
    }

    
}
