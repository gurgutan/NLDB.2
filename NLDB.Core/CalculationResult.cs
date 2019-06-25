using System;

namespace NLDB
{
    public enum ResultType { Success, Error };

    public enum OperationType
    {
        None,
        FileReading,
        FileWriting,
        TextNormalization,
        TextSplitting,
        WordsExtraction,
        DistancesCalculation,
        SimilarityCalculation,
        GrammarCreating,
        GrammarLoading
    };

    public enum ExecuteMode
    {
        Silent, Verbose, Debug
    };


    public class CalculationResult
    {
        public OperationType ProcessingType;
        public ResultType ResultType { get; private set; }
        public readonly Engine Engine;
        public object Data;

        public CalculationResult(Engine engine, OperationType processingType, ResultType resultType, object data = null)
        {
            ProcessingType = processingType;
            ResultType = resultType;
            Engine = engine;
            Data = data;
        }

        public CalculationResult Then(OperationType ptype, params object[] parameters)
        {
            if (Engine.ExecuteMode == ExecuteMode.Verbose)
                Console.WriteLine($"Операция {ProcessingType} завершилась с результатом {ResultType}");
            if (ResultType == ResultType.Error)
            {
                Console.WriteLine($"Операция {ptype} не выполнена");
                return this;
            }
            else
                return Engine.Execute(ptype, parameters);
        }

        public override string ToString()
        {
            return $"{ProcessingType.ToString()} - {ResultType.ToString()}";
        }
    }
}
