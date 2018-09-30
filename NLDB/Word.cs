using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    /// <summary>
    /// Класс для представления сущности Слово. Слово является иерархической древовидной структурой, элементами которой являются другие Слова (дочерние).
    /// Типовой пример Слова - Предложение (Слово ранга 2), состоящее из слов (Слово ранга 1), состоящих из букв (Слов ранга 0).
    /// В отличие от класса Term, Word не содержит линейного текста, но содержит вспомогательные структуры данных для использования в быстрых алгоритмах.
    /// Дочерние Слова представлены своими идентификаторами.
    /// </summary>
    [Serializable]
    public class Word
    {
        //TODO: Заменить поля на свойства. В целях быстродействия в режиме отладки пока оставляю поля.
        /// <summary>
        /// Ранг слова, соответствует высоте дерева, корнем которого является данное Слово. Минимальный ранг = 0.
        /// </summary>
        public int rank;
        /// <summary>
        /// Уникальный идентификатор слова. Выдается при создании или после проверки того, что нет полного аналога данного слова.
        /// Аналог определяется через Equals
        /// </summary>
        public int id;
        /// <summary>
        /// Массив идентификаторов дочерних Слов
        /// </summary>
        public int[] childs;
        /// <summary>
        /// Список Родителей данного слова. Одно слово может входить как дочернее в несколько других слов.
        /// </summary>
        public List<int> parents;
        /// <summary>
        /// Поле используется только для хранения текстового представления Слова ранга 0. Для слов ранга>0 symbol=null
        /// </summary>
        public string symbol = null;
        ///Длина слова, используемая для преобразования Слова в разреженный вектор.
        ///Пока не используется
        //public static readonly int WORD_SIZE = 1024;


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

        //public void AddParent(int p)
        //{
        //    parents.Add(p);
        //    //Array.Resize(ref parents, parents.Length + 1);
        //    //parents[parents.Length - 1] = p;
        //}

        /// <summary>
        /// Хэш-код слова зависит от ранга rank, id, childs. Используется алгоритм известный под именем Ly
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

        /// <summary>
        /// Сравнение по дочерним элементам
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Метод возвращает представление слова в виде разреженного вектора.
        /// </summary>
        /// <returns></returns>
        //public Dictionary<int[], float> AsSparseVector()
        //{
        //    Dictionary<int[], float> vector = new Dictionary<int[], float>();
        //    int pos = 0;
        //    foreach (var c in childs)
        //    {
        //        vector.Add(new int[] { 0, c * Word.WORD_SIZE + pos }, 1);
        //        pos++;
        //    }
        //    return vector;
        //}

        public override string ToString()
        {
            return "{" + childs.Aggregate("", (c, n) => c == "" ? n.ToString() : c + "," + n.ToString()) + "}";
        }
    }

    /// <summary>
    /// Класс-сравнитель для использования в Linq.Distinct
    /// </summary>
    public class WordComparer : IEqualityComparer<Word>
    {
        public bool Equals(Word x, Word y)
        {
            return x.id == y.id;
        }

        public int GetHashCode(Word obj)
        {
            return obj.GetHashCode();
        }
    }
}
