using System;
using System.Collections.Generic;
using System.Linq;

namespace NLDB.DAL
{
    /// <summary>
    /// Класс для представления сущности Терм. Терм является древовидной структурой и состоит из дочерних термов.
    /// Ранг дочерних термов на единицу меньше ранга родительского терма. Терм, не имеющий дочерних имеет ранг=0. 
    /// Типовой пример Терма в естественном языке - предложение, состоящее из дочерних термов - слов, состоящих, 
    /// в свою очередь из букв - Термов нулевого ранга.
    /// </summary>
    public class Term : IComparable
    {
        //TODO: Скрыть поля - сделать свойства. Пока для скорости во время отладки оставляю поля.
        /// <summary>
        /// Ранг Терма. Минимальный ранг = 0, максимальный ранг соответствует высоте дерева, корнем которого является данный Терм.
        /// </summary>
        public int rank;
        /// <summary>
        /// Идентификатор Терма. Действует соглашение: если терм не сопоставлен со Словом (не идентифицирован), то id=0.
        /// При идентификации id присвается id сопоставленного Слова.
        /// </summary>
        public int id;
        /// <summary>
        /// Оценка уверенности в том, что данный Терм сопоставлен Слову правильно
        /// </summary>
        public float confidence;
        /// <summary>
        /// Линейный исходный текст Терма
        /// </summary>
        public string text;

        private List<Term> childs;
        public List<Term> Childs { get { return childs; } }

        private readonly List<Term> parents;
        public List<Term> Parents { get { return parents; } }

        /// <summary>
        /// Признак того, что данный терм был уже идентифицирован и сопоставлен с некоторым словом. 
        /// Т.е. рассчитаны и присвоены значения свойствам id, Confidence
        /// </summary>
        public bool Identified { get; set; }

        public Term(int _rank, int _id, float _confidence, string _text, IEnumerable<Term> _childs = null, IEnumerable<Term> _parents = null)
        {
            Identified = false;
            rank = _rank;
            id = _id;
            confidence = _confidence;
            text = _text;
            if (_childs != null) childs = new List<Term>(_childs);
            if (_parents != null) parents = new List<Term>(_parents);
        }

        public override string ToString()
        {
            if (rank == 0)
                return text;
            else
                return "{" + childs.Aggregate("", (c, n) => c == "" ? n.ToString() : c + "" + n.ToString()) + "}";
        }

        /// <summary>
        /// Проверка наличия дочернего Терма t в данном Терме.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool Contains(Term t)
        {
            //TODO: оптимизировать поиск без существенных потерь памяти
            return childs.Any(c => c.id == t.id);
        }

        /// <summary>
        /// Количество дочерних Термов
        /// </summary>
        public int Count { get { return childs == null ? 0 : childs.Count; } }

        public override bool Equals(object obj)
        {
            Term t = (Term)obj;
            //Если указан id, то сравниваем по id (состав может отличаться). Такой способ нужен для поиска с неизвестным составом
            if (id == t.id) return true;
            //Если один из термов не имеет дочерних слов (и id не равны), то термы не равны
            if (t.childs == null || childs == null || t.Count == 0 || Count == 0) return false;
            //Если длины слов не равны то слова не равны
            if (t.Count != Count) return false;
            //Если тривиальные случаи не сработали, то сравниваем каждое дочернее слово с соответствующим
            for (int i = 0; i < Count; i++)
                if (t.childs[i] != childs[i]) return false;
            return true;
        }

        /// <summary>
        /// Хэш-код терма зависит от ранга rank, id, childs 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if (childs == null || Count == 0)
                return id;
            int hash = rank * 1664525;
            for (int i = 0; i < Count; i++)
            {
                hash += childs[i].id + 1013904223;
                hash *= 1664525;
            }
            return hash;
        }

        public int CompareTo(object obj) => confidence.CompareTo(obj);
    }

    /// <summary>
    /// Клоасс-сравнитель для Term, используется для вычисления неповторяющихся последовательностей Термов
    /// </summary>
    public class TermComparer : IEqualityComparer<Term>
    {
        public bool Equals(Term x, Term y) => x.id == y.id;

        public int GetHashCode(Term obj) => obj.GetHashCode();
    }
}
