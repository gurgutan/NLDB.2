using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace NLDB.DAL
{
    [Table("SplittersTable")]
    public class SplittersTable
    {
        [PrimaryKey]
        public int Rank { get; set; }
        public string Expr { get; set; }

    }
}
