using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    [Serializable]
    public class Word : ISerializable
    {
        public int id;
        public int rank;
        public int[] childs = new int[0];
        public List<int> parents = new List<int>();
        //public int[] parents = new int[0];

        public Word(int _id, int _rank, int[] _childs, int[] _parents)
        {
            id = _id;
            rank = _rank;
            childs = _childs;
            parents = new List<int>(_parents);
        }

        protected Word(SerializationInfo info, StreamingContext context)
        {
            id = info.GetInt32("Id");
            childs = (int[])info.GetValue("Childs", typeof(int[]));
            parents = (List<int>)info.GetValue("Parents", typeof(List<int>));
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Id", id);
            info.AddValue("Childs", childs);
            info.AddValue("Parents", parents);
        }

        public void AddParent(int p)
        {
            parents.Add(p);
            //Array.Resize(ref parents, parents.Length + 1);
            //parents[parents.Length - 1] = p;
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
            if (childs == null || w.childs == null) return false;
            if (childs.Length != w.childs.Length) return false;
            for (int i = 0; i < childs.Length; i++)
                if (childs[i] != w.childs[i]) return false;
            return true;
        }
    }
}
