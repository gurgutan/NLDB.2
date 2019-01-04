using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    //Структура для накопления значений с последующим вычислением среднего
    public struct MeanValue
    {
        public int count;
        public float sum;
        public MeanValue(int c, float s)
        {
            count = c;
            sum = s;
        }

        public float Value()
        {
            return sum / count;
        }
    }
}
