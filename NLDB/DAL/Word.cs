using System.Linq;
using SQLite;

namespace NLDB.DAL
{
    [Table("WordsTable")]
    public class Word
    {
        [AutoIncrement, PrimaryKey]
        public int Id { get; set; }

        [Indexed, NotNull]
        public int Rank { get; set; }

        [Indexed]
        public string Symbol { get; set; }

        [Indexed]
        public string Childs { get; set; }

        /// <summary>
        /// Хэш-код слова зависит от ранга rank, id, childs. Используется алгоритм известный под именем Ly
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if (Childs == null || Childs.Length == 0) return Id;
            return Childs
                .Split(',')
                .Select(i => int.Parse(i))
                .Aggregate(Rank * 1664525,
                    (current, next) => (current + 1013904223 + next) * 1664525);
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
    }
}
