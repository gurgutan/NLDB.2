using System.Linq;

namespace NLDB.DAL
{
    public class Word
    {
        public int Id { get; set; }

        public int Rank { get; set; }

        public string Symbol { get; set; }

        public string Childs { get; set; }

        public int[] ChildsId => string.IsNullOrEmpty(Childs) ? new int[0] : Childs.Split(',').Select(s => int.Parse(s)).ToArray();

        public bool HasChilds
        {
            get { return !string.IsNullOrEmpty(Childs); }
        }

        public Word()
        {
        }

        public Word(int id, int rank, string symbol, string childs)
        {
            Id = id;
            Rank = rank;
            Symbol = symbol;
            Childs = childs;
        }

        public Term AsTerm(Engine engine) => engine.ToTerm(this);

        /// <summary>
        /// Хэш-код слова зависит от ранга rank, id, childs. Используется алгоритм известный под именем Ly
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if (Childs == null || Childs.Length == 0) return Id;
            return ChildsId.Aggregate(Rank * 1664525, (current, next) => (current + 1013904223 + next) * 1664525);
        }

        /// <summary>
        /// Сравнение по дочерним элементам
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            Word w = (Word)obj;
            if (Id == w.Id) return true;
            if (Childs?.Length != w.Childs?.Length) return false;
            return Childs == w.Childs;
        }

        public override string ToString()
        {
            return "[" + Childs + "]";
        }

    }
}
