using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB.DAL
{
    //Структура для хранения и поиска в коллекции массивов целых значений
    public struct Sequence
    {
        public int[] sequence;

        /// <summary>
        /// Быстрое создание Цепочки из готового массива
        /// </summary>
        /// <param name="_sequence"></param>
        public Sequence(int[] _sequence)
        {
            sequence = _sequence;
        }

        public int Length
        {
            get { return sequence.Length; }
        }

        public int this[int i]
        {
            get { return sequence[i]; }
            set { sequence[i] = value; }
        }

        public override int GetHashCode()
        {
            if (sequence == null || sequence.Length == 0) return 0;
            int hash = 0;
            for (int i = 0; i < sequence.Length; i++)
            {
                hash += sequence[i] + 1013904223;
                hash *= 1664525;
            }
            return hash;
        }

        public override bool Equals(object obj)
        {
            Sequence l = (Sequence)obj;
            if (sequence == null || l.sequence == null) return false;
            if (sequence.Length != l.sequence.Length) return false;
            for (int i = 0; i < sequence.Length; i++)
                if (sequence[i] != l.sequence[i]) return false;
            return true;
        }
    }

}
