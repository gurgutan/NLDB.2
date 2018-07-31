using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    [Serializable]
    public class Word
    {
        public int rank;
        public int id;
        public int[] childs;
        public List<int> parents;
        public string symbol = null;

        //public int[] parents = new int[0];

        public Word(int _id, int _rank, string _symbol, int[] _childs, int[] _parents)
        {
            //if (_childs == null) throw new ArgumentNullException("_childs не может быть равен null. Используйте int[0] вместо null");
            id = _id;
            rank = _rank;
            symbol = _symbol;
            childs = _childs;
            if (_parents != null)
                parents = new List<int>(_parents);
        }

        public void AddParent(int p)
        {
            parents.Add(p);
            //Array.Resize(ref parents, parents.Length + 1);
            //parents[parents.Length - 1] = p;
        }

        /// <summary>
        /// Хэш-код слова зависит от ранга rank, id, childs 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if (childs == null || childs.Length == 0) return id;
            int hash = rank * 1664525;
            for (int i = 0; i < childs.Length; i++)
            {
                hash += childs[i] + 1013904223;
                hash *= 1664525;
            }
            return hash;
        }

        public override bool Equals(object obj)
        {
            Word w = (Word)obj;
            if (id == w.id) return true;
            //if (childs == null || w.childs == null) return false;
            if (childs?.Length != w.childs?.Length) return false;
            for (int i = 0; i < childs.Length; i++)
                if (childs[i] != w.childs[i]) return false;
            return true;
        }

        public Dictionary<int[], float> AsSparseVector()
        {
            Dictionary<int[], float> vector = new Dictionary<int[], float>();
            int pos = 0;
            foreach (var c in childs)
            {
                vector.Add(new int[] { 0, c * Language.WORD_SIZE + pos }, 1);
                pos++;
            }
            return vector;
        }

        public override string ToString()
        {
            return "{" + childs.Aggregate("", (c, n) => c == "" ? n.ToString() : c + "," + n.ToString()) + "}";
        }
    }
}
