using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public class Word
    {
        public int id;
        public int[] childs;

        public Word(int _id)
        {
            this.id = _id;
            this.childs = new int[0];
        }

        public Word(int _id, int[] _childs)
        {
            this.id = _id;
            this.childs = _childs;
        }

        public override bool Equals(object obj)
        {
            Word w = (Word)obj;
            //Если указан id, то сравниваем по id (состав может отличаться). Такой способ нужен для поиска с неизвестным составом
            if (this.id == w.id) return true;
            //Если длины слов не равны то слова не равны
            if (w.childs.Length != this.childs.Length) return false;
            if (w.childs.Length == 0) return w.id == this.id;
            for (int i = 0; i < this.childs.Length; i++)
                if (w.childs[i] != this.childs[i]) return false;
            return true;
        }

        public override int GetHashCode()
        {
            if (this.childs.Length == 0)
                return this.id;
            int hash = 0;
            for (int i = 0; i < this.childs.Length; i++)
            {
                hash += this.childs[i] + 1013904223;
                hash *= 1664525;
            }
            return hash;
        }

    }


}
