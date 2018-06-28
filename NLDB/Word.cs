using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    //TODO: Создать модульные тесты для Word
    [Serializable]
    public struct WordLink : ISerializable
    {
        public int Id;
        public int Pos;
        //public float value;

        public WordLink(int _id, int _pos)
        {
            Id = _id;
            Pos = _pos;
            //value = _v;
        }

        public WordLink(SerializationInfo info, StreamingContext context)
        {
            Id = info.GetInt32("Id");
            Pos = info.GetInt32("Pos");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Id", Id);
            info.AddValue("Pos", Pos);
        }
    }

    [Serializable]
    public class Word : ISerializable
    {
        public int Id;
        public int[] Childs;
        public List<WordLink> Parents;

        public Word(int _id)
        {
            this.Id = _id;
            this.Childs = new int[0];
            this.Parents = new List<WordLink>();
        }

        /// <summary>
        /// Быстрая инициализация слова через передачу(присвоение) ссылки на массив дочерних _childs
        /// </summary>
        /// <param name="_id"></param>
        /// <param name="_childs"></param>
        public Word(int _id, int[] _childs)
        {
            this.Id = _id;
            this.Childs = _childs;
            this.Parents = new List<WordLink>();
        }

        /// <summary>
        /// Инициализация слова через копирование коллекций _childs и _parents
        /// </summary>
        /// <param name="_id">Id слова</param>
        /// <param name="_childs">коллекция идентификаторов дочерних слов</param>
        /// <param name="_parents">коллекция линков на родительские слова</param>
        public Word(int _id, IEnumerable<int> _childs, IEnumerable<WordLink> _parents)
        {
            this.Id = _id;
            this.Childs = _childs.ToArray();
            this.Parents = _parents.ToList();
        }

        protected Word(SerializationInfo info, StreamingContext context)
        {
            Id = info.GetInt32("Id");
            Childs = (int[])info.GetValue("Childs", typeof(int[]));
            Parents = (List<WordLink>)info.GetValue("Parents", typeof(List<WordLink>));
        }
        
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Id", Id);
            info.AddValue("Childs", Childs);
            info.AddValue("Parents", Parents);
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
