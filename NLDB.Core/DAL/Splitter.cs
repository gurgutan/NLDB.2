using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB.DAL
{
    public class Splitter
    {
        public Splitter(int rank, string expr)
        {
            Rank = rank;
            Expression = expr;
        }

        public int Rank { get; set; }
        public string Expression { get; set; }

    }
}
