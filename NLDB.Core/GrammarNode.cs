using System.Collections.Generic;
using System.Linq;

namespace NLDB
{
    //TODO: Включить Grammar в состав класса DataBase. Реализовать сохранение/загрузку грамматики

    /// <summary>
    /// Элемент (узел) грамматики. Содержит идентификатор слова и ссылки на следующие узлы
    /// </summary>
    public class GrammarNode
    {
        public int id;      // уникальный идентификатор элемента 
        public int word_id; // идент-р слова, соответствующего данному элементу грамматики

        public Dictionary<int, GrammarNode> Followers
        {
            get
            {
                return followers;
            }
        }

        public GrammarNode(int _id, int _word_id = -1)
        {
            id = _id;
            if (_word_id == -1) word_id = id;
            else word_id = _word_id;
        }

        public override string ToString()
        {
            if (followers.Count == 0)
                return word_id.ToString();
            else if (followers.Count == 1)
                return word_id.ToString() + "->" + followers.Values.First().ToString();
            else
            {
                var f = followers.Values.ToList();
                string ending = "";
                if (f.Count > 4)
                {
                    f = f.Take(4).ToList();
                    ending = "...";
                }
                string followers_str = f.Aggregate("", (c, n) => c + (c != "" ? "|" : "") + n.ToString());
                return word_id.ToString() + "->" + "(" + followers_str + ending + ")";
            }
        }

        public override int GetHashCode()
        {
            return id;
        }

        public override bool Equals(object obj)
        {
            return (obj as GrammarNode).id == id;
        }

        //--------------------------------------------------------------------------------------------------------
        // Частные свойства
        private readonly Dictionary<int, GrammarNode> followers = new Dictionary<int, GrammarNode>();
    }
}
