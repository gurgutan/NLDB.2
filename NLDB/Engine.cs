using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLDB.DAL;

namespace NLDB
{
    public enum ProcessingType { WordsExtraction, WordsMean, WordsSimilarity };

    public class Engine
    {
        private DataBase db;

        private Parser[] parsers = null;
        private string dbpath;

        public Engine(string dbpath)
        {
            this.dbpath = dbpath;
        }

        public Parser[] Parsers
        {
            get
            {
                if (parsers == null)
                    parsers = db.Table<SplittersTable>().OrderBy(r => r.Rank).Select(r => new Parser(r.Expr)).ToArray();
                return parsers;
            }
        }

        public void Create()
        {
            throw new NotImplementedException();
        }

        public Engine Preprocessing(string trainfile)
        {
            throw new NotImplementedException();
        }

        public Engine Preprocessing(ProcessingType ptype)
        {
            throw new NotImplementedException();
        }

        internal List<Term> Recognize(string text, int count)
        {
            throw new NotImplementedException();
        }

        internal List<Term> Similars(string text, int count)
        {
            throw new NotImplementedException();
        }
    }
}
