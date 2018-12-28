using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace NLDB.DAL
{
    [Table("SplittersTable")]
    public class Splitter
    {
        public Splitter()
        {
        }

        public Splitter(int rank, string expr)
        {
            Rank = rank;
            Expr = expr;
        }

        [PrimaryKey, Unique]
        public int Rank { get; set; }
        public string Expr { get; set; }

    }
}
