using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public struct WordLink
    {
        public int id;
        public byte pos;
        //public float value;

        public WordLink(int _id, byte _pos, float _v)
        {
            id = _id;
            pos = _pos;
            //value = _v;
        }
    }

    public class Word
    {
        public int id;
        public int[] childs;
        public List<WordLink> parents = new List<WordLink>();

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

        public IEnumerable<int> ParentCodes
        {
            get { return parents.Select(p => p.id); }
        }

        public Dictionary<int[], double> AsSparseVector()
        {
            Dictionary<int[], double> vector = new Dictionary<int[], double>();
            int pos = 0;
            foreach (var c in childs)
            {
                vector.Add(new int[] { 0, c * Language.WORD_SIZE + pos }, 1.0);
                pos++;
            }
            return vector;
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
