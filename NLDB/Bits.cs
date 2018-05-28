using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public class Bits32
    {
        int v;
        public Bits32(int _v)
        {
            v = _v;
        }

        public int Get(int i)
        {
            return (v >> i) & 1;
        }

        public void Set(int i)
        {
            v |= (1 << i);
        }
        public void Unset(int i)
        {
            v &= (0 << i);
        }
    }
}
