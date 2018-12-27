using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    /// <summary>
    /// Тип результата обработки данных
    /// </summary>
    public enum ResultType { Success, Error };

    public class CalculationResult
    {
        public ProcessingType ProcessingType;
        public ResultType ResultType { get; private set; }
        public object Data;
        public readonly Engine Engine;

        public CalculationResult(Engine engine, ProcessingType processingType, ResultType resultType, object data)
        {
            ProcessingType = processingType;
            ResultType = resultType;
            Data = data;
            Engine = engine;
        }

        public CalculationResult Execute(ProcessingType ptype)
        {
            Engine.So
            return Engine.Execute(ptype);
        }
    }
}
