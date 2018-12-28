using System;

namespace NLDB
{
    /// <summary>
    /// Тип результата обработки данных
    /// </summary>
    public enum ResultType { Success, Error };

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

        public CalculationResult Then(OperationType ptype)
        {
            if (Engine.ExecuteMode == ExecuteMode.Verbose)
                Console.WriteLine($"Операция {ProcessingType} завершилась с результатом {ResultType}");
            if (ResultType == ResultType.Error)
            {
                Console.WriteLine($"Операция {ptype} не выполнена");
                return this;
            }
            else
                return Engine.Execute(ptype, Engine.Data);
        }
    }
}
