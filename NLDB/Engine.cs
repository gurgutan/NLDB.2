using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLDB.DAL;

namespace NLDB
{
    public enum ProcessingType { TextNormalization, TextSplitting, WordsExtraction, WordsMean, WordsSimilarity };

    public class Engine
    {
        private readonly DataBase db;
        private readonly string dbpath;
        private Parser[] parsers;

        private string sourceFile;
        public string SourceFile
        {
            get => sourceFile;
            set => sourceFile = value;
        }
        public string ResultFile;

        private object Data;

        public CalculationResult CalculationResult { get; private set; }

        public Engine(string dbpath)
        {
            this.dbpath = dbpath;
            db = new DataBase(dbpath);
        }

        public void Create()
        {
            db.Create();
            parsers = db.Table<SplittersTable>().OrderBy(r => r.Rank).Select(r => new Parser(r.Expr)).ToArray();
        }

        public CalculationResult Execute(ProcessingType ptype)
        {
            switch (ptype)
            {
                case ProcessingType.TextNormalization: return NormilizeText();
                case ProcessingType.TextSplitting: return SplitText();
                default: throw new NotImplementedException();
            }
        }


        internal List<Term> Recognize(string text, int count)
        {
            throw new NotImplementedException();
        }

        internal List<Term> Similars(string text, int count)
        {
            throw new NotImplementedException();
        }

        private CalculationResult NormilizeText()
        {
            if (!File.Exists(SourceFile))
                return new CalculationResult(this, ProcessingType.TextNormalization, ResultType.Error, null);
            ResultFile = Path.ChangeExtension(SourceFile, ProcessingType.TextNormalization.ToString());
            using (StreamReader reader = File.OpenText(SourceFile))
            using (StreamWriter writer = File.CreateText(ResultFile))
                writer.Write(Parser.Normilize(reader.ReadToEnd()));
            return new CalculationResult(this, ProcessingType.TextNormalization, ResultType.Error, ResultFile);
        }

        private CalculationResult SplitText()
        {
            if (!File.Exists(SourceFile))
                return new CalculationResult(this, ProcessingType.TextSplitting, ResultType.Success, null);
            
        }

        //----------------------------------------------------------------------------------------------------------------------------------
        //private const int TEXT_BUFFER_SIZE = 1 << 18;

    }
}
