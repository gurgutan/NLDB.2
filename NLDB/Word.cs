using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    //TODO: Создать модульные тесты для Word
    [Serializable]
    public struct WordLink
    {
        public int Id;
        public int pos;
        //public float value;

        public WordLink(int _id, int _pos)
        {
            Id = _id;
            pos = _pos;
            //value = _v;
        }
    }

    [Serializable]
    public class Word
    {
        public int Id;
        public int[] Childs;
        public List<WordLink> Parents = new List<WordLink>();

        public Word(int _id)
        {
            this.Id = _id;
            this.Childs = new int[0];
        }

        public Word(int _id, int[] _childs)
        {
            this.Id = _id;
            this.Childs = _childs;
        }

        public void AddParent(int _id, int _pos)
        {
            Parents.Add(new WordLink(_id, _pos));
        }

        public IEnumerable<int> ParentCodes
        {
            get { return Parents.Select(p => p.Id); }
        }

        public Dictionary<int[], double> AsSparseVector()
        {
            Dictionary<int[], double> vector = new Dictionary<int[], double>();
            int pos = 0;
            foreach (var c in Childs)
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
            if (this.Id == w.Id) return true;
            //Если длины слов не равны то слова не равны
            if (w.Childs.Length != this.Childs.Length) return false;
            if (w.Childs.Length == 0) return w.Id == this.Id;
            for (int i = 0; i < this.Childs.Length; i++)
                if (w.Childs[i] != this.Childs[i]) return false;
            return true;
        }

        public override int GetHashCode()
        {
            if (this.Childs.Length == 0)
                return this.Id;
            int hash = 0;
            for (int i = 0; i < this.Childs.Length; i++)
            {
                hash += this.Childs[i] + 1013904223;
                hash *= 1664525;
            }
            return hash;
        }
    }
}
